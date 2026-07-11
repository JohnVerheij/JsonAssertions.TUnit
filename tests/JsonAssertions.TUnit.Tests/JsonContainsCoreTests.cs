using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// Framework-agnostic tests for the public core <see cref="JsonEquivalence.ContainsAll(string, string, JsonEquivalenceOptions)"/>
/// subset entry points: the string and element overloads, the empty-difference (contained) result,
/// and the collect-all behavior returning every difference.
/// </summary>
[Category("Smoke")]
[Timeout(5_000)]
internal sealed class JsonContainsCoreTests
{
    [Test]
    public async Task ContainsAll_String_Contained_ReturnsEmpty(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var differences = JsonEquivalence.ContainsAll(
            """{"a":1}""", """{"a":1,"b":2}""", new JsonEquivalenceOptions());
        await Assert.That(differences).IsEmpty();
    }

    [Test]
    public async Task ContainsAll_String_CollectsEveryDifference(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var differences = JsonEquivalence.ContainsAll(
            """{"a":1,"b":2,"c":3}""", """{"a":9}""", new JsonEquivalenceOptions());

        // a differs (value), b and c are missing: three differences, not one.
        await Assert.That(differences.Count).IsEqualTo(3);
        await Assert.That(differences.Any(d => string.Equals(d.Path, "a", StringComparison.Ordinal) && d.Kind == JsonDifferenceKind.Value)).IsTrue();
        await Assert.That(differences.Any(d => string.Equals(d.Path, "b", StringComparison.Ordinal) && d.Kind == JsonDifferenceKind.MissingProperty)).IsTrue();
        await Assert.That(differences.Any(d => string.Equals(d.Path, "c", StringComparison.Ordinal) && d.Kind == JsonDifferenceKind.MissingProperty)).IsTrue();
    }

    [Test]
    public async Task ContainsAll_Element_Overload(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var expected = JsonDocument.Parse("""{"a":1}""");
        using var actual = JsonDocument.Parse("""{"a":1,"b":2}""");
        var differences = JsonEquivalence.ContainsAll(
            expected.RootElement, actual.RootElement, new JsonEquivalenceOptions());
        await Assert.That(differences).IsEmpty();
    }

    [Test]
    public async Task ContainsAll_NullArguments_Throw(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(() => JsonEquivalence.ContainsAll(null!, "{}", new JsonEquivalenceOptions())).Throws<ArgumentNullException>();
        await Assert.That(() => JsonEquivalence.ContainsAll("{}", null!, new JsonEquivalenceOptions())).Throws<ArgumentNullException>();
        await Assert.That(() => JsonEquivalence.ContainsAll("{}", "{}", null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task ContainsMismatch_RendersEveryDifference(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var differences = JsonEquivalence.ContainsAll(
            """{"a":1,"b":2}""", """{"a":9}""", new JsonEquivalenceOptions());
        var message = JsonFailureMessage.ContainsMismatch(differences);
        await Assert.That(message).Contains("\"a\"", StringComparison.Ordinal);
        await Assert.That(message).Contains("\"b\"", StringComparison.Ordinal);
    }
}
