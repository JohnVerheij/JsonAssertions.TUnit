using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JsonAssertions;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// Tests for the framework-agnostic <see cref="JsonShape"/> core: the value-kind predicate
/// and the array-length / non-empty / empty predicates, including the kind-mismatch case
/// (which must return <see langword="false"/> rather than throw).
/// </summary>
[Category("Smoke")]
[Timeout(5_000)]
internal sealed class JsonShapeTests
{
    private static JsonElement Parse(string json, string property)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty(property).Clone();
    }

    [Test]
    public async Task IsKind_MatchingKind_ReturnsTrue(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonShape.IsKind(Parse("""{"v":[1]}""", "v"), JsonValueKind.Array)).IsTrue();
    }

    [Test]
    public async Task IsKind_DifferentKind_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonShape.IsKind(Parse("""{"v":"x"}""", "v"), JsonValueKind.Array)).IsFalse();
    }

    [Test]
    public async Task IsArrayOfLength_MatchingLength_ReturnsTrue(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonShape.IsArrayOfLength(Parse("""{"v":[1,2,3]}""", "v"), 3)).IsTrue();
    }

    [Test]
    public async Task IsArrayOfLength_DifferentLength_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonShape.IsArrayOfLength(Parse("""{"v":[1,2]}""", "v"), 3)).IsFalse();
    }

    [Test]
    public async Task IsArrayOfLength_NotAnArray_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonShape.IsArrayOfLength(Parse("""{"v":"x"}""", "v"), 0)).IsFalse();
    }

    [Test]
    public async Task IsNonEmptyArray_NonEmpty_ReturnsTrue(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonShape.IsNonEmptyArray(Parse("""{"v":[1]}""", "v"))).IsTrue();
    }

    [Test]
    public async Task IsNonEmptyArray_Empty_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonShape.IsNonEmptyArray(Parse("""{"v":[]}""", "v"))).IsFalse();
    }

    [Test]
    public async Task IsNonEmptyArray_NotAnArray_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonShape.IsNonEmptyArray(Parse("""{"v":42}""", "v"))).IsFalse();
    }

    [Test]
    public async Task IsEmptyArray_Empty_ReturnsTrue(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonShape.IsEmptyArray(Parse("""{"v":[]}""", "v"))).IsTrue();
    }

    [Test]
    public async Task IsEmptyArray_NonEmpty_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonShape.IsEmptyArray(Parse("""{"v":[1]}""", "v"))).IsFalse();
    }

    [Test]
    public async Task IsEmptyArray_NotAnArray_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonShape.IsEmptyArray(Parse("""{"v":{}}""", "v"))).IsFalse();
    }
}
