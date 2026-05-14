using System.Threading;
using System.Threading.Tasks;
using JsonAssertions.TUnit;
using PublicApiGenerator;
using SnapshotAssertions.TUnit;

namespace JsonAssertions.TUnit.SnapshotTests;

/// <summary>
/// Pins the public API surface of the shipped <c>JsonAssertions.TUnit</c> assembly using
/// <c>SnapshotAssertions.TUnit</c>'s <c>MatchesSnapshot()</c> chain. Any change to a public
/// type, member, signature, attribute, or visibility, across both the <c>JsonAssertions</c>
/// core namespace and the <c>JsonAssertions.TUnit</c> adapter namespace, produces a diff
/// against the <c>.expected.txt</c> baseline under <c>Snapshots/</c> and fails the test until
/// the snapshot is explicitly re-accepted (write the new content to the expected path, or run
/// with <c>SNAPSHOT_ACCEPT=1</c> to auto-write).
/// </summary>
/// <remarks>
/// <para>
/// Stronger than ApiCompat's per-version baseline check because this snapshot fires on every
/// PR, not just at pack time.
/// </para>
/// <para>
/// Cross-package dogfooding: this project consumes <c>SnapshotAssertions.TUnit</c> as a
/// downstream user of the family would, demonstrating that the family's snapshot helper is
/// suitable for the package's own public-API surface checks.
/// </para>
/// </remarks>
[Category("Smoke")]
[Timeout(10_000)]
internal sealed class PublicApiTests
{
    /// <summary>
    /// Pins the public surface of the single shipped assembly: the <c>JsonAssertions.JsonPath</c>
    /// core type plus the <c>JsonAssertions.TUnit.JsonPropertyAssertions</c> adapter class and
    /// its <c>[GenerateAssertion]</c>-emitted entry points.
    /// </summary>
    [Test]
    public async Task JsonAssertionsTUnitPublicApiHasNotChangedAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var assembly = typeof(JsonPropertyAssertions).Assembly;
        // Normalize line endings so the snapshot baseline survives both Linux CI (LF native)
        // and Windows local dev (CRLF native). Without this, PublicApiGenerator emits the
        // platform's native EOL while the committed .expected.txt baseline is always LF
        // (per .gitattributes), and Windows local runs would diff against the CI-accepted
        // baseline.
        var publicApi = assembly.GeneratePublicApi().ReplaceLineEndings("\n");

        await Assert.That(publicApi).MatchesSnapshot();
    }
}
