using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JsonAssertions;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// Tests for the framework-agnostic <see cref="JsonValueComparison"/> core: each <c>Matches</c>
/// overload against a matching value, a value mismatch of the same kind, and a kind mismatch
/// (which must return <see langword="false"/> rather than throw).
/// </summary>
[Category("Smoke")]
[Timeout(5_000)]
internal sealed class JsonValueComparisonTests
{
    private static JsonElement Parse(string json, string property)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty(property).Clone();
    }

    [Test]
    public async Task Matches_String_EqualValue_ReturnsTrue(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":"alice"}""", "v"), "alice")).IsTrue();
    }

    [Test]
    public async Task Matches_String_DifferentValue_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":"alice"}""", "v"), "bob")).IsFalse();
    }

    [Test]
    public async Task Matches_String_KindMismatch_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":42}""", "v"), "42")).IsFalse();
    }

    [Test]
    public async Task Matches_Boolean_EqualValue_ReturnsTrue(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":true}""", "v"), true)).IsTrue();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":false}""", "v"), false)).IsTrue();
    }

    [Test]
    public async Task Matches_Boolean_DifferentValue_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":true}""", "v"), false)).IsFalse();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":false}""", "v"), true)).IsFalse();
    }

    [Test]
    public async Task Matches_Boolean_KindMismatch_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":"true"}""", "v"), true)).IsFalse();
    }

    [Test]
    public async Task Matches_Number_EqualValue_ReturnsTrue(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":30}""", "v"), 30d)).IsTrue();
    }

    [Test]
    public async Task Matches_Number_DifferentValue_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":30}""", "v"), 25d)).IsFalse();
    }

    [Test]
    public async Task Matches_Number_KindMismatch_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":"30"}""", "v"), 30d)).IsFalse();
    }
}
