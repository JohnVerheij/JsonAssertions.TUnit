using System;
using System.Threading;
using System.Threading.Tasks;
using TUnit.Assertions.Exceptions;
using static JsonAssertions.TUnit.Tests.RealUseTestSupport;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// The JSON assertions composed the way tests write them: <c>Assert.Multiple</c>, <c>.And</c> / <c>.Or</c> chains,
/// <c>Because(...)</c>, and the extraction methods inside an <c>Assert.Multiple</c> scope.
/// </summary>
[Timeout(10_000)]
internal sealed class RealUseCompositionTests
{
    [Test]
    public async Task AssertMultiple_ReportsEveryJsonFailure(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var ex = await Assert.That(async () =>
        {
            using (Assert.Multiple())
            {
                await Assert.That(Body).HasJsonProperty("missing");
                await Assert.That(Body).HasJsonValue("user.age", 30);
            }
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("missing");
        await Assert.That(ex.Message).Contains("but found 31");
    }

    [Test]
    public async Task Or_OnAStringOrAnElement_PassesWhenOneLinkPasses(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = System.Text.Json.JsonDocument.Parse(Body);

        await Assert.That(Body).HasJsonProperty("missing").Or.HasJsonProperty("user");
        await Assert.That(document.RootElement).HasJsonProperty("user").Or.HasJsonProperty("missing");
    }

    /// <summary>
    /// TUnit bug, pinned as a canary: on an <see cref="System.Net.Http.HttpResponseMessage"/> (a non-sealed receiver,
    /// so its generated assertions are covariant) <c>.Or</c> behaves as <c>.And</c>, for TUnit's own HTTP assertions
    /// as well. The cause is in TUnit's <c>AssertionContext.Map</c>, which drops the pending combiner.
    /// When this test fails after a TUnit update, TUnit has fixed it: delete this test and the README's known-issue
    /// paragraph about <c>.Or</c> on a response. The failure blocks that update until then, on purpose.
    /// </summary>
    [Test]
    public async Task Or_OnAResponse_FailsWhenOnlyOneLinkPasses_UntilTUnitFixesMap(CancellationToken ct)
    {
        using var response = Buffered(Body);

        await Assert.That(async () => await Assert.That(response).HasJsonProperty("missing", ct).Or.HasJsonProperty("user", ct))
            .Throws<AssertionException>();
        await Assert.That(async () => await Assert.That(response).HasJsonProperty("user", ct).Or.HasJsonProperty("missing", ct))
            .Throws<AssertionException>();
        await Assert.That(async () => await Assert.That(response).IsNotFound().Or.IsOk()).Throws<AssertionException>();
    }

    [Test]
    public async Task And_OnAResponse_NeedsBothLinks(CancellationToken ct)
    {
        using var response = Buffered(Body);

        await Assert.That(response).HasJsonProperty("user", ct).And.HasJsonValue("user.age", 31, ct);
        await Assert.That(async () => await Assert.That(response).HasJsonProperty("user", ct).And.HasJsonProperty("missing", ct))
            .Throws<AssertionException>();
    }

    [Test]
    public async Task Because_AppearsInTheFailure(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await Assert.That(async () => await Assert.That(Body).HasJsonProperty("missing").Because("the API returns it"))
            .Throws<AssertionException>().WithMessageContaining("because the API returns it", StringComparison.Ordinal);
    }

    /// <summary>
    /// By design of C# and TUnit, pinned for the README: an extraction method that throws inside an
    /// <c>Assert.Multiple</c> scope after an assertion in the same scope failed is replaced by the scope's own
    /// exception when the scope is disposed, so the extraction error is not reported. Extract before the scope.
    /// </summary>
    [Test]
    public async Task ExtractionErrorAfterAFailureInAssertMultiple_IsReplacedByTheScopeFailure(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var ex = await Assert.That(async () =>
        {
            using (Assert.Multiple())
            {
                await Assert.That(Body).HasJsonProperty("missing");
                _ = Body.GetJsonValue<int>("absent");
            }
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).DoesNotContain("absent");
    }

    [Test]
    public async Task ExtractionErrorAloneInAssertMultiple_Propagates(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await Assert.That(() =>
        {
            using (Assert.Multiple())
            {
                _ = Body.GetJsonValue<int>("absent");
            }
        }).Throws<JsonExtractionException>();
    }
}
