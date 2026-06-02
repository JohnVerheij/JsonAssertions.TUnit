using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TUnit.Assertions.Exceptions;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// End-to-end tests for <c>[*]</c> wildcard paths on the two assertions that support them:
/// <c>HasJsonProperty</c> (every expanded element must have the property) and
/// <c>HasJsonValueMatching</c> (the predicate must hold on every expanded element). An empty array
/// passes vacuously; a failure names which element failed.
/// </summary>
[Category("Smoke")]
[Timeout(5_000)]
internal sealed class WildcardAssertionTests
{
    [Test]
    public async Task HasJsonProperty_WildcardAllPresent_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That("""[{"id":1},{"id":2}]""").HasJsonProperty("[*].id");
    }

    [Test]
    public async Task HasJsonProperty_WildcardOneMissing_FailsNamingIndex(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That("""[{"id":1},{"other":2}]""").HasJsonProperty("[*].id");
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("[1]");
    }

    [Test]
    public async Task HasJsonProperty_WildcardEmptyArray_PassesVacuously(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That("[]").HasJsonProperty("[*].id");
    }

    [Test]
    public async Task HasJsonValueMatching_WildcardAllMatch_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That("""[{"on":true},{"on":true}]""")
            .HasJsonValueMatching("[*].on", v => v.ValueKind == JsonValueKind.True);
    }

    [Test]
    public async Task HasJsonValueMatching_WildcardOneFails_FailsNamingIndex(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That("""[{"on":true},{"on":false}]""")
                .HasJsonValueMatching("[*].on", v => v.ValueKind == JsonValueKind.True);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("[1]");
    }

    [Test]
    public async Task HasJsonValueMatching_WildcardEmptyArray_PassesVacuously(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That("[]").HasJsonValueMatching("[*].on", v => v.ValueKind == JsonValueKind.True);
    }
}
