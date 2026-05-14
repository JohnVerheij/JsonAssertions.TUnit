using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JsonAssertions;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// Tests for the framework-agnostic <see cref="JsonPath"/> core: dot-separated path
/// navigation over a <see cref="JsonElement"/>, the JSONPath-style <c>$.</c> prefix, the
/// non-object short-circuit, and the argument-validation paths.
/// </summary>
[Category("Smoke")]
[Timeout(5_000)]
internal sealed class JsonPathTests
{
    [Test]
    public async Task Exists_NestedProperty_ReturnsTrue(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"user":{"address":{"city":"Amsterdam"}}}""");

        await Assert.That(JsonPath.Exists(document.RootElement, "user.address.city")).IsTrue();
    }

    [Test]
    public async Task Exists_RootLevelProperty_ReturnsTrue(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"name":"alice"}""");

        await Assert.That(JsonPath.Exists(document.RootElement, "name")).IsTrue();
    }

    [Test]
    public async Task Exists_MissingProperty_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"user":{"name":"alice"}}""");

        await Assert.That(JsonPath.Exists(document.RootElement, "user.email")).IsFalse();
    }

    [Test]
    public async Task Exists_PathTraversesNonObject_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"user":"alice"}""");

        // "user" resolves to a string, so navigating into "user.name" cannot continue.
        await Assert.That(JsonPath.Exists(document.RootElement, "user.name")).IsFalse();
    }

    [Test]
    public async Task Exists_LeadingDollarPrefix_IsAcceptedAndIgnored(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"user":{"name":"alice"}}""");

        await Assert.That(JsonPath.Exists(document.RootElement, "$.user.name")).IsTrue();
    }

    [Test]
    public async Task Exists_EmptyPath_ThrowsArgumentException(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"name":"alice"}""");
        var root = document.RootElement;

        await Assert.That(() => JsonPath.Exists(root, "   ")).Throws<ArgumentException>();
    }

    [Test]
    public async Task Exists_EmptySegment_ThrowsArgumentException(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"user":{"name":"alice"}}""");
        var root = document.RootElement;

        await Assert.That(() => JsonPath.Exists(root, "user..name")).Throws<ArgumentException>();
    }
}
