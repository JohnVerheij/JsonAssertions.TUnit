using System;
using System.Threading;
using System.Threading.Tasks;
using JsonAssertions;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// Direct tests for the framework-agnostic <see cref="JsonEquivalence"/> string entry point and the
/// <see cref="JsonEquivalenceOptions"/> guards, which the adapter assertions do not reach (they parse
/// and call the <c>JsonElement</c> overload). Confirms the string overload parses both sides, returns
/// <see langword="null"/> on equivalence, a <see cref="JsonDifference"/> otherwise, and validates its
/// arguments.
/// </summary>
[Category("Smoke")]
[Timeout(5_000)]
internal sealed class JsonEquivalenceTests
{
    [Test]
    public async Task Compare_Strings_Equivalent_ReturnsNull(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var difference = JsonEquivalence.Compare("""{"a":1,"b":2}""", """{"b":2,"a":1}""", new JsonEquivalenceOptions());
        await Assert.That(difference).IsNull();
    }

    [Test]
    public async Task Compare_Strings_Different_ReturnsDifference(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var difference = JsonEquivalence.Compare("""{"a":1}""", """{"a":2}""", new JsonEquivalenceOptions());
        await Assert.That(difference).IsNotNull();
        await Assert.That(difference!.Kind).IsEqualTo(JsonDifferenceKind.Value);
        await Assert.That(difference.Path).IsEqualTo("a");
    }

    [Test]
    public async Task Compare_NullExpected_Throws(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(() => JsonEquivalence.Compare(null!, "{}", new JsonEquivalenceOptions()))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Compare_NullActual_Throws(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(() => JsonEquivalence.Compare("{}", null!, new JsonEquivalenceOptions()))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Compare_NullOptions_Throws(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(() => JsonEquivalence.Compare("{}", "{}", null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    [Arguments(" ")]
    [Arguments("")]
    [Arguments(null)]
    public async Task Options_IgnorePath_NullOrWhitespace_Throws(string? path, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(() => new JsonEquivalenceOptions().IgnorePath(path!)).Throws<ArgumentException>();
    }

    [Test]
    public async Task Options_FluentChaining_RecordsState(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var options = new JsonEquivalenceOptions().IgnorePath("a").IgnoreArrayOrder();
        await Assert.That(options.IgnoredPaths).Contains("a");
        await Assert.That(options.IgnoreArrayOrderEnabled).IsTrue();
    }
}
