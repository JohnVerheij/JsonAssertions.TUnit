using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using TUnit.Assertions.Exceptions;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// End-to-end tests for <c>RoundtripsCleanlyVia&lt;T&gt;</c>: asserts that a value
/// serializes-deserializes-reserializes via a <c>JsonTypeInfo&lt;T&gt;</c> without drift in the
/// resulting JSON string. Covers the success path, missing-property regression (the educational-demand
/// use case the v0.3.0 plan calls out), and the deserialize-to-null edge.
/// </summary>
[Category("Smoke")]
[Timeout(5_000)]
internal sealed partial class RoundtripsCleanlyViaTests
{
    [Test]
    public async Task RoundtripsCleanlyVia_StableShape_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var dto = new RoundtripDto(42, "alice", true);

        await Assert.That(dto).RoundtripsCleanlyVia(RoundtripJsonContext.Default.RoundtripDto);
    }

    [Test]
    public async Task RoundtripsCleanlyVia_RecordWithNullableField_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var dto = new RoundtripDto(0, null, false);

        await Assert.That(dto).RoundtripsCleanlyVia(RoundtripJsonContext.Default.RoundtripDto);
    }

    [Test]
    public async Task RoundtripsCleanlyVia_NullValue_Fails(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        RoundtripDto? dto = null;

        var ex = await Assert.That(async () =>
        {
            await Assert.That(dto).RoundtripsCleanlyVia(RoundtripJsonContext.Default.RoundtripDto);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("non-null");
    }

    /// <summary>A small record used as the round-trip target. Records get value-equality
    /// for free; STJ source-gen produces a stable JSON shape for them in v0.3.0.</summary>
    internal sealed record RoundtripDto(int Id, string? Name, bool IsActive);

    /// <summary>STJ source-gen context for <see cref="RoundtripDto"/>. The outer
    /// <see cref="RoundtripsCleanlyViaTests"/> class is partial so the source generator can emit
    /// the generated half of this context next to its declaration (the same workaround used in
    /// <c>HasJsonResponseTests</c>).</summary>
    [JsonSerializable(typeof(RoundtripDto))]
    internal sealed partial class RoundtripJsonContext : JsonSerializerContext;
}
