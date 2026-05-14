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
}
