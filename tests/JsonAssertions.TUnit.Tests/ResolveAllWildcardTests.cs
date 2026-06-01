using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JsonAssertions;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// Tests for the wildcard path engine: <see cref="JsonPath.ContainsWildcard"/> and
/// <see cref="JsonPath.ResolveAll"/> (segment tokenization, the property / index / wildcard
/// expansion branches, empty-array and non-array edges, nesting, and argument validation).
/// </summary>
[Category("Smoke")]
[Timeout(5_000)]
internal sealed class ResolveAllWildcardTests
{
    [Test]
    public async Task ContainsWildcard_Detection(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonPath.ContainsWildcard("[*].id")).IsTrue();
        await Assert.That(JsonPath.ContainsWildcard("items[0].id")).IsFalse();
        await Assert.That(() => JsonPath.ContainsWildcard(null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task ResolveAll_NoWildcard_SingleResolution(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"user":{"name":"alice"}}""");

        var resolutions = JsonPath.ResolveAll(document.RootElement, "user.name");

        await Assert.That(resolutions.Count).IsEqualTo(1);
        await Assert.That(resolutions[0].Found).IsTrue();
        await Assert.That(resolutions[0].Element.GetString()).IsEqualTo("alice");
    }

    [Test]
    public async Task ResolveAll_RootSelf_ReturnsRoot(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"a":1}""");

        var resolutions = JsonPath.ResolveAll(document.RootElement, "$");

        await Assert.That(resolutions.Count).IsEqualTo(1);
        await Assert.That(resolutions[0].Found).IsTrue();
        await Assert.That(resolutions[0].Element.ValueKind).IsEqualTo(JsonValueKind.Object);
    }

    [Test]
    public async Task ResolveAll_WildcardAllElements_Found(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""[{"id":1},{"id":2},{"id":3}]""");

        var resolutions = JsonPath.ResolveAll(document.RootElement, "[*].id");

        await Assert.That(resolutions.Count).IsEqualTo(3);
        await Assert.That(resolutions[0].Found).IsTrue();
        await Assert.That(resolutions[0].ResolvedPrefix).IsEqualTo("[0].id");
        await Assert.That(resolutions[2].ResolvedPrefix).IsEqualTo("[2].id");
    }

    [Test]
    public async Task ResolveAll_WildcardOneElementMissingProperty_ReportsThatIndex(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""[{"id":1},{"other":2}]""");

        var resolutions = JsonPath.ResolveAll(document.RootElement, "[*].id");

        await Assert.That(resolutions.Count).IsEqualTo(2);
        await Assert.That(resolutions[0].Found).IsTrue();
        await Assert.That(resolutions[1].Found).IsFalse();
        await Assert.That(resolutions[1].ResolvedPrefix).IsEqualTo("[1]");
        await Assert.That(resolutions[1].FailedSegment).IsEqualTo("id");
    }

    [Test]
    public async Task ResolveAll_EmptyArrayWildcard_ZeroResolutions(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("[]");

        var resolutions = JsonPath.ResolveAll(document.RootElement, "[*].id");

        await Assert.That(resolutions.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ResolveAll_WildcardOnNonArray_SingleNotFound(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"a":1}""");

        var resolutions = JsonPath.ResolveAll(document.RootElement, "[*]");

        await Assert.That(resolutions.Count).IsEqualTo(1);
        await Assert.That(resolutions[0].Found).IsFalse();
        await Assert.That(resolutions[0].FailedSegment).IsEqualTo("[*]");
        await Assert.That(resolutions[0].ContainerKind).IsEqualTo(JsonValueKind.Object);
    }

    [Test]
    public async Task ResolveAll_NestedWildcard(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"cycles":[{"cycleId":"a"},{"cycleId":"b"}]}""");

        var resolutions = JsonPath.ResolveAll(document.RootElement, "cycles[*].cycleId");

        await Assert.That(resolutions.Count).IsEqualTo(2);
        await Assert.That(resolutions[0].ResolvedPrefix).IsEqualTo("cycles[0].cycleId");
        await Assert.That(resolutions[1].Element.GetString()).IsEqualTo("b");
    }

    [Test]
    public async Task ResolveAll_MultipleWildcards_CartesianExpansion(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""[{"tags":["a","b"]},{"tags":["c"]}]""");

        var resolutions = JsonPath.ResolveAll(document.RootElement, "[*].tags[*]");

        await Assert.That(resolutions.Count).IsEqualTo(3);
        await Assert.That(resolutions[0].ResolvedPrefix).IsEqualTo("[0].tags[0]");
        await Assert.That(resolutions[1].ResolvedPrefix).IsEqualTo("[0].tags[1]");
        await Assert.That(resolutions[2].ResolvedPrefix).IsEqualTo("[1].tags[0]");
    }

    [Test]
    public async Task ResolveAll_LiteralIndexIntoArray_Found(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"items":[{"id":7}]}""");

        var resolutions = JsonPath.ResolveAll(document.RootElement, "items[0].id");

        await Assert.That(resolutions.Count).IsEqualTo(1);
        await Assert.That(resolutions[0].Found).IsTrue();
        await Assert.That(resolutions[0].Element.GetInt32()).IsEqualTo(7);
        await Assert.That(resolutions[0].ResolvedPrefix).IsEqualTo("items[0].id");
    }

    [Test]
    public async Task ResolveAll_IndexOutOfRange_NotFound(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("[1,2]");

        var resolutions = JsonPath.ResolveAll(document.RootElement, "[5]");

        await Assert.That(resolutions.Count).IsEqualTo(1);
        await Assert.That(resolutions[0].Found).IsFalse();
    }

    [Test]
    public async Task ResolveAll_IndexOnNonArray_NotFound(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"a":1}""");

        var resolutions = JsonPath.ResolveAll(document.RootElement, "[0]");

        await Assert.That(resolutions[0].Found).IsFalse();
    }

    [Test]
    public async Task ResolveAll_PropertyOnNonObject_NotFound(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"a":5}""");

        var resolutions = JsonPath.ResolveAll(document.RootElement, "a.b");

        await Assert.That(resolutions[0].Found).IsFalse();
        await Assert.That(resolutions[0].ResolvedPrefix).IsEqualTo("a");
    }

    [Test]
    [Arguments("items[")]
    [Arguments("[]")]
    [Arguments("[x]")]
    [Arguments("[0]x")]
    public async Task ResolveAll_MalformedPath_Throws(string path, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"items":[]}""");
        await Assert.That(() => JsonPath.ResolveAll(document.RootElement, path)).Throws<ArgumentException>();
    }

    [Test]
    public async Task ResolveAll_NullOrWhitespacePath_Throws(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("{}");
        await Assert.That(() => JsonPath.ResolveAll(document.RootElement, null!)).Throws<ArgumentException>();
        await Assert.That(() => JsonPath.ResolveAll(document.RootElement, "   ")).Throws<ArgumentException>();
    }
}
