using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JsonAssertions;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// Tests for the framework-agnostic <see cref="JsonPath"/> core: the <see cref="JsonPath.Resolve"/>
/// resolver and its <see cref="JsonPathResolution"/> failure-point context, the
/// <see cref="JsonPath.Exists"/> shorthand, the JSONPath-style <c>$.</c> prefix, the
/// non-object short-circuit, and the argument-validation paths.
/// </summary>
[Category("Smoke")]
[Timeout(5_000)]
internal sealed class JsonPathTests
{
    [Test]
    public async Task Resolve_NestedProperty_FindsElement(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"user":{"address":{"city":"Amsterdam"}}}""");

        var resolution = JsonPath.Resolve(document.RootElement, "user.address.city");

        await Assert.That(resolution.Found).IsTrue();
        await Assert.That(resolution.Element.GetString()).IsEqualTo("Amsterdam");
        await Assert.That(resolution.ResolvedPrefix).IsEqualTo("user.address.city");
        await Assert.That(resolution.FailedSegment).IsNull();
    }

    [Test]
    public async Task Resolve_LeadingDollarPrefix_IsAcceptedAndIgnored(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"user":{"name":"alice"}}""");

        await Assert.That(JsonPath.Resolve(document.RootElement, "$.user.name").Found).IsTrue();
    }

    [Test]
    public async Task Resolve_MissingPropertyOnNestedObject_ReportsPrefixAndSegment(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"user":{"address":{"street":"Main"}}}""");

        var resolution = JsonPath.Resolve(document.RootElement, "user.address.city");

        await Assert.That(resolution.Found).IsFalse();
        await Assert.That(resolution.ResolvedPrefix).IsEqualTo("user.address");
        await Assert.That(resolution.FailedSegment).IsEqualTo("city");
        await Assert.That(resolution.ContainerKind).IsEqualTo(JsonValueKind.Object);
    }

    [Test]
    public async Task Resolve_MissingPropertyOnRoot_ReportsEmptyPrefix(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"name":"alice"}""");

        var resolution = JsonPath.Resolve(document.RootElement, "user");

        await Assert.That(resolution.Found).IsFalse();
        await Assert.That(resolution.ResolvedPrefix).IsEqualTo(string.Empty);
        await Assert.That(resolution.FailedSegment).IsEqualTo("user");
        await Assert.That(resolution.ContainerKind).IsEqualTo(JsonValueKind.Object);
    }

    [Test]
    public async Task Resolve_PathTraversesNonObject_ReportsContainerKind(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"user":"alice"}""");

        var resolution = JsonPath.Resolve(document.RootElement, "user.name");

        await Assert.That(resolution.Found).IsFalse();
        await Assert.That(resolution.ResolvedPrefix).IsEqualTo("user");
        await Assert.That(resolution.FailedSegment).IsEqualTo("name");
        await Assert.That(resolution.ContainerKind).IsEqualTo(JsonValueKind.String);
    }

    [Test]
    public async Task Resolve_RootIsNotAnObject_ReportsEmptyPrefixAndRootKind(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""[1,2,3]""");

        var resolution = JsonPath.Resolve(document.RootElement, "user");

        await Assert.That(resolution.Found).IsFalse();
        await Assert.That(resolution.ResolvedPrefix).IsEqualTo(string.Empty);
        await Assert.That(resolution.ContainerKind).IsEqualTo(JsonValueKind.Array);
    }

    [Test]
    public async Task Exists_PropertyPresent_ReturnsTrue(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"user":{"name":"alice"}}""");

        await Assert.That(JsonPath.Exists(document.RootElement, "user.name")).IsTrue();
    }

    [Test]
    public async Task Exists_PropertyAbsent_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"user":{"name":"alice"}}""");

        await Assert.That(JsonPath.Exists(document.RootElement, "user.email")).IsFalse();
    }

    [Test]
    public async Task Resolve_EmptyPath_ThrowsArgumentException(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"name":"alice"}""");
        var root = document.RootElement;

        await Assert.That(() => JsonPath.Resolve(root, "   ")).Throws<ArgumentException>();
    }

    [Test]
    public async Task Resolve_EmptySegment_ThrowsArgumentException(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"user":{"name":"alice"}}""");
        var root = document.RootElement;

        await Assert.That(() => JsonPath.Resolve(root, "user..name")).Throws<ArgumentException>();
    }

    [Test]
    public async Task Resolve_ArrayIndex_FindsElement(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"items":[{"id":"a"},{"id":"b"}]}""");

        var resolution = JsonPath.Resolve(document.RootElement, "items[1].id");

        await Assert.That(resolution.Found).IsTrue();
        await Assert.That(resolution.Element.GetString()).IsEqualTo("b");
        await Assert.That(resolution.ResolvedPrefix).IsEqualTo("items[1].id");
    }

    [Test]
    public async Task Resolve_NestedArrayIndices_FindsElement(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"matrix":[[1,2],[3,4]]}""");

        var resolution = JsonPath.Resolve(document.RootElement, "matrix[1][0]");

        await Assert.That(resolution.Found).IsTrue();
        await Assert.That(resolution.Element.GetInt32()).IsEqualTo(3);
        await Assert.That(resolution.ResolvedPrefix).IsEqualTo("matrix[1][0]");
    }

    [Test]
    public async Task Resolve_ArrayIndexOutOfRange_ReportsArrayContainerKind(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"items":[{"id":"a"}]}""");

        var resolution = JsonPath.Resolve(document.RootElement, "items[5]");

        await Assert.That(resolution.Found).IsFalse();
        await Assert.That(resolution.ResolvedPrefix).IsEqualTo("items");
        await Assert.That(resolution.FailedSegment).IsEqualTo("[5]");
        await Assert.That(resolution.ContainerKind).IsEqualTo(JsonValueKind.Array);
    }

    [Test]
    public async Task Resolve_IndexOnNonArray_ReportsObjectContainerKind(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"user":{"name":"alice"}}""");

        var resolution = JsonPath.Resolve(document.RootElement, "user[0]");

        await Assert.That(resolution.Found).IsFalse();
        await Assert.That(resolution.ResolvedPrefix).IsEqualTo("user");
        await Assert.That(resolution.FailedSegment).IsEqualTo("[0]");
        await Assert.That(resolution.ContainerKind).IsEqualTo(JsonValueKind.Object);
    }

    [Test]
    public async Task Resolve_BareDollar_ResolvesToRootElement(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""[1,2,3]""");

        var resolution = JsonPath.Resolve(document.RootElement, "$");

        await Assert.That(resolution.Found).IsTrue();
        await Assert.That(resolution.Element.ValueKind).IsEqualTo(JsonValueKind.Array);
        await Assert.That(resolution.ResolvedPrefix).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Resolve_DollarIndex_ResolvesAgainstRootArray(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""[{"id":"a"},{"id":"b"}]""");

        var resolution = JsonPath.Resolve(document.RootElement, "$[1].id");

        await Assert.That(resolution.Found).IsTrue();
        await Assert.That(resolution.Element.GetString()).IsEqualTo("b");
        await Assert.That(resolution.ResolvedPrefix).IsEqualTo("[1].id");
    }

    [Test]
    public async Task Resolve_BareBracketIndex_ResolvesAgainstRootArray(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""[10,20,30]""");

        var resolution = JsonPath.Resolve(document.RootElement, "[2]");

        await Assert.That(resolution.Found).IsTrue();
        await Assert.That(resolution.Element.GetInt32()).IsEqualTo(30);
    }

    [Test]
    public async Task Resolve_UnclosedBracket_ThrowsArgumentException(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"items":[]}""");
        var root = document.RootElement;

        await Assert.That(() => JsonPath.Resolve(root, "items[0")).Throws<ArgumentException>();
    }

    [Test]
    public async Task Resolve_EmptyBracket_ThrowsArgumentException(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"items":[]}""");
        var root = document.RootElement;

        await Assert.That(() => JsonPath.Resolve(root, "items[]")).Throws<ArgumentException>();
    }

    [Test]
    public async Task Resolve_NonNumericIndex_ThrowsArgumentException(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"items":[]}""");
        var root = document.RootElement;

        await Assert.That(() => JsonPath.Resolve(root, "items[x]")).Throws<ArgumentException>();
    }

    [Test]
    public async Task Resolve_NegativeIndex_ThrowsArgumentException(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"items":[]}""");
        var root = document.RootElement;

        await Assert.That(() => JsonPath.Resolve(root, "items[-1]")).Throws<ArgumentException>();
    }

    [Test]
    public async Task Resolve_PropertySegmentDirectlyAfterIndex_ThrowsArgumentException(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"items":[{"name":"a"}]}""");
        var root = document.RootElement;

        // 'items[0]name' is malformed: a property name following an index requires a dot
        // separator. The walker must reject it rather than silently parse 'name' as another
        // property segment.
        await Assert.That(() => JsonPath.Resolve(root, "items[0]name")).Throws<ArgumentException>();
    }

    [Test]
    public async Task Resolve_BareDollarFollowedByLetter_ThrowsArgumentException(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"user":{}}""");
        var root = document.RootElement;

        // '$user' is not a valid JSONPath root form: the root reference must be '$', '$.',
        // or '$['.
        await Assert.That(() => JsonPath.Resolve(root, "$user")).Throws<ArgumentException>();
    }

    [Test]
    public async Task Resolve_LeadingDot_ThrowsArgumentException(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"user":{}}""");
        var root = document.RootElement;

        await Assert.That(() => JsonPath.Resolve(root, ".user")).Throws<ArgumentException>();
    }

    [Test]
    public async Task Resolve_TrailingDot_ThrowsArgumentException(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"user":{}}""");
        var root = document.RootElement;

        await Assert.That(() => JsonPath.Resolve(root, "user.")).Throws<ArgumentException>();
    }

    [Test]
    public async Task Resolve_DollarDotIndex_StripsRootAndDot(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"user":{"name":"alice"}}""");

        var resolution = JsonPath.Resolve(document.RootElement, "$.user.name");

        await Assert.That(resolution.Found).IsTrue();
        await Assert.That(resolution.Element.GetString()).IsEqualTo("alice");
    }
}
