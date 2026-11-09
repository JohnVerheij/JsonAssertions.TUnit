using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// Tests for the framework-agnostic <see cref="JsonRenderers"/> static factory. The renderers
/// return <c>Func&lt;string, string&gt;</c> projections suitable for composition with any
/// framework-agnostic consumer (notably <c>SnapshotAssertions.TUnit</c>'s
/// <c>MatchesSnapshot(Func&lt;string, string&gt;)</c> overload, though the composition is verified
/// at the consumer's call site, not here — JsonAssertions stays decoupled from
/// SnapshotAssertions per the v0.6 cross-package references rule).
/// </summary>
[Category("Smoke")]
[Timeout(5_000)]
internal sealed partial class JsonRenderersTests
{
    [Test]
    public async Task ReformatJson_PropertyOrderAndWhitespaceVariations_ProduceCanonicalForm(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // Property order reversed (Name before Id) and extra whitespace in input. The canonical
        // form orders properties per the source-gen context's emission order (record positional
        // declaration: Id, Name).
        const string input = """{ "Name" : "alice" ,  "Id" : 42 }""";
        var renderer = JsonRenderers.ReformatJson(RendererTestJsonContext.Default.RendererTestDto);

        var canonical = renderer(input);

        await Assert.That(canonical).IsEqualTo("""{"Id":42,"Name":"alice"}""");
    }

    [Test]
    public async Task ReformatJson_AlreadyCanonical_IsIdempotent(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        const string canonical = """{"Id":42,"Name":"alice"}""";
        var renderer = JsonRenderers.ReformatJson(RendererTestJsonContext.Default.RendererTestDto);

        var reformatted = renderer(canonical);

        await Assert.That(reformatted).IsEqualTo(canonical);
    }

    [Test]
    public async Task ReformatJson_FactoryReturnsReusableRenderer_AcrossMultipleInvocations(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var renderer = JsonRenderers.ReformatJson(RendererTestJsonContext.Default.RendererTestDto);

        // Same renderer instance can be invoked many times.
        var first = renderer("""{"Id":1,"Name":"a"}""");
        var second = renderer("""{"Id":2,"Name":"b"}""");
        var third = renderer("""{"Id":1,"Name":"a"}""");

        await Assert.That(first).IsEqualTo("""{"Id":1,"Name":"a"}""");
        await Assert.That(second).IsEqualTo("""{"Id":2,"Name":"b"}""");
        await Assert.That(third).IsEqualTo(first);
    }

    [Test]
    public async Task ReformatJson_LiteralNullInput_CanonicalizesToNullLiteral(CancellationToken ct)
    {
        // Input is the JSON literal "null"; the renderer must short-circuit to the "null"
        // literal rather than serializing default(T).
        ct.ThrowIfCancellationRequested();
        var renderer = JsonRenderers.ReformatJson(RendererTestJsonContext.Default.RendererTestDto);

        var canonical = renderer("null");

        await Assert.That(canonical).IsEqualTo("null");
    }

    /// <summary>A string that starts with a byte order mark, as <c>Encoding.UTF8.GetString</c> keeps it, is read without
    /// it, as at every other entry point that takes JSON text.</summary>
    [Test]
    public async Task ReformatJson_LeadingByteOrderMark_IsSkipped(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var renderer = JsonRenderers.ReformatJson(RendererTestJsonContext.Default.RendererTestDto);

        var canonical = renderer("\uFEFF{\"Name\":\"alice\",\"Id\":42}");

        await Assert.That(canonical).IsEqualTo("""{"Id":42,"Name":"alice"}""");
    }

    /// <summary>A small record used as the renderer's deserialization target.</summary>
    internal sealed record RendererTestDto(int Id, string Name);

    /// <summary>STJ source-gen context for <see cref="RendererTestDto"/>.</summary>
    [JsonSerializable(typeof(RendererTestDto))]
    internal sealed partial class RendererTestJsonContext : JsonSerializerContext;
}
