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

    [Test]
    public async Task MatchesAny_String_OneCandidateMatches_ReturnsTrue(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.MatchesAny(Parse("""{"v":"b"}""", "v"), "a", "b", "c")).IsTrue();
    }

    [Test]
    public async Task MatchesAny_String_NoMatch_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.MatchesAny(Parse("""{"v":"z"}""", "v"), "a", "b", "c")).IsFalse();
    }

    [Test]
    public async Task MatchesAny_String_KindMismatch_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.MatchesAny(Parse("""{"v":42}""", "v"), "42")).IsFalse();
    }

    [Test]
    public async Task MatchesAny_String_NullCandidates_Throws(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var element = Parse("""{"v":"a"}""", "v");
        await Assert.That(() => JsonValueComparison.MatchesAny(element, (string[])null!)).Throws<System.ArgumentNullException>();
    }

    [Test]
    public async Task MatchesAny_Number_OneCandidateMatches_ReturnsTrue(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.MatchesAny(Parse("""{"v":503}""", "v"), 200d, 503d)).IsTrue();
    }

    [Test]
    public async Task MatchesAny_Number_NoMatch_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.MatchesAny(Parse("""{"v":418}""", "v"), 200d, 503d)).IsFalse();
    }

    [Test]
    public async Task MatchesAny_Number_KindMismatch_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.MatchesAny(Parse("""{"v":"503"}""", "v"), 503d)).IsFalse();
    }

    [Test]
    public async Task MatchesAny_Number_NullCandidates_Throws(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var element = Parse("""{"v":1}""", "v");
        await Assert.That(() => JsonValueComparison.MatchesAny(element, (double[])null!)).Throws<System.ArgumentNullException>();
    }
}
