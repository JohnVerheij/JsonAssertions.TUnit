using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TUnit.Assertions.Exceptions;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// End-to-end tests for the <c>[GenerateAssertion]</c>-emitted <c>ContainsJson</c> subset chains
/// over a JSON <see cref="string"/>, a <see cref="JsonElement"/>, and an
/// <see cref="HttpResponseMessage"/>. Exercises object subset (extra actual properties ignored),
/// nested subset, positional array-prefix subset, the all-differences failure output, and
/// composition with <c>IgnorePath</c> / <c>IgnoreArrayOrder</c>.
/// </summary>
[Category("Smoke")]
[Timeout(5_000)]
internal sealed class JsonContainsAssertionsTests
{
    /// <summary>Builds a JSON HTTP response with the given body.</summary>
    /// <param name="body">The JSON body.</param>
    /// <returns>The response.</returns>
    private static HttpResponseMessage JsonResponse(string body)
        => new() { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    [Test]
    public async Task Subset_ExtraActualProperties_AreIgnored(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That("""{"status":"Healthy","totalDurationMs":12,"diagnostics":{"host":"a"}}""")
            .ContainsJson("""{"status":"Healthy"}""");
    }

    [Test]
    public async Task Subset_MissingProperty_Fails(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
            await Assert.That("""{"status":"Healthy"}""").ContainsJson("""{"status":"Healthy","version":"1.2"}"""))
            .Throws<AssertionException>();
        await Assert.That(ex!.Message).Contains("\"version\"", StringComparison.Ordinal);
        await Assert.That(ex.Message).Contains("property missing", StringComparison.Ordinal);
    }

    [Test]
    public async Task Subset_Nested_ExtraNestedProperties_AreIgnored(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That("""{"user":{"id":7,"name":"x","lastSeen":"2026-01-01"}}""")
            .ContainsJson("""{"user":{"id":7,"name":"x"}}""");
    }

    [Test]
    public async Task Subset_Array_PositionalPrefix_LongerActualPasses(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // Assert entries[0]'s shape; the actual array has more elements.
        await Assert.That("""{"entries":[{"name":"db","status":"Healthy"},{"name":"cache"},{"name":"queue"}]}""")
            .ContainsJson("""{"entries":[{"name":"db","status":"Healthy"}]}""");
    }

    [Test]
    public async Task Subset_Array_TooShort_Fails(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
            await Assert.That("""{"items":[1]}""").ContainsJson("""{"items":[1,2]}"""))
            .Throws<AssertionException>();
        await Assert.That(ex!.Message).Contains("at least 2 element(s)", StringComparison.Ordinal);
    }

    [Test]
    public async Task Subset_ReportsAllDifferences_NotJustFirst(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // Two independently wrong fields must both appear in one failure.
        var ex = await Assert.That(async () =>
            await Assert.That("""{"status":"Degraded","count":3}""")
                .ContainsJson("""{"status":"Healthy","count":5,"missing":true}"""))
            .Throws<AssertionException>();
        await Assert.That(ex!.Message).Contains("\"status\"", StringComparison.Ordinal);
        await Assert.That(ex.Message).Contains("\"count\"", StringComparison.Ordinal);
        await Assert.That(ex.Message).Contains("\"missing\"", StringComparison.Ordinal);
    }

    [Test]
    public async Task Subset_NumberFormAndPropertyOrderIndependent(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That("""{"b":2,"a":1.0}""").ContainsJson("""{"a":1,"b":2}""");
    }

    [Test]
    public async Task Subset_ValueMismatch_ReportsPathAndKind(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
            await Assert.That("""{"a":{"b":1}}""").ContainsJson("""{"a":{"b":2}}"""))
            .Throws<AssertionException>();
        await Assert.That(ex!.Message).Contains("\"a.b\"", StringComparison.Ordinal);
        await Assert.That(ex.Message).Contains("value differs", StringComparison.Ordinal);
    }

    [Test]
    public async Task Subset_KindMismatch_DoesNotDescend(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
            await Assert.That("""{"a":5}""").ContainsJson("""{"a":{"b":1}}"""))
            .Throws<AssertionException>();
        await Assert.That(ex!.Message).Contains("JSON kind differs", StringComparison.Ordinal);
        // Must NOT try to report a.b (the actual side is not an object).
        await Assert.That(ex.Message).DoesNotContain("\"a.b\"", StringComparison.Ordinal);
    }

    [Test]
    public async Task Subset_EmptyObject_MatchesAnyObject(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That("""{"a":1,"b":2}""").ContainsJson("{}");
    }

    [Test]
    public async Task Subset_IgnorePath_Composes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That("""{"status":"Healthy","ts":"2026-01-01"}""")
            .ContainsJson("""{"status":"Healthy","ts":"IGNORED"}""", o => o.IgnorePath("ts"));
    }

    [Test]
    public async Task Subset_IgnoreArrayOrder_MultisetSubset(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // Order-insensitive subset: expected elements each match some actual element; actual larger.
        await Assert.That("""{"tags":["c","a","b"]}""")
            .ContainsJson("""{"tags":["a","b"]}""", o => o.IgnoreArrayOrder());
    }

    [Test]
    public async Task Subset_JsonElementReceiver(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"cycleGuid":"g","cycleId":42,"extra":true}""");
        await Assert.That(document.RootElement).ContainsJson("""{"cycleGuid":"g","cycleId":42}""");
    }

    [Test]
    public async Task Subset_HttpResponseReceiver_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var response = JsonResponse("""{"status":"Healthy","totalDurationMs":3,"entries":[{"name":"db","status":"Healthy"}]}""");
        await Assert.That(response)
            .ContainsJson("""{"status":"Healthy","entries":[{"name":"db","status":"Healthy"}]}""", ct);
    }

    [Test]
    public async Task Subset_HttpResponseReceiver_MissingField_Fails(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var response = JsonResponse("""{"status":"Healthy"}""");
        var ex = await Assert.That(async () =>
            await Assert.That(response).ContainsJson("""{"status":"Healthy","entries":[]}""", ct))
            .Throws<AssertionException>();
        await Assert.That(ex!.Message).Contains("\"entries\"", StringComparison.Ordinal);
    }

    [Test]
    public async Task Subset_HttpResponseReceiver_WithConfigure_IgnoresPath(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var response = JsonResponse("""{"status":"Healthy","totalDurationMs":42}""");
        await Assert.That(response)
            .ContainsJson("""{"status":"Healthy","totalDurationMs":0}""", o => o.IgnorePath("totalDurationMs"), ct);
    }

    [Test]
    public async Task Subset_HttpResponse_NonJsonBody_ExplainedFailure(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var response = JsonResponse("not json");
        var ex = await Assert.That(async () =>
            await Assert.That(response).ContainsJson("""{"a":1}""", ct))
            .Throws<AssertionException>();
        await Assert.That(ex!.Message).Contains("parseable JSON", StringComparison.Ordinal);
    }

    [Test]
    public async Task Subset_MalformedExpected_ExplainedFailure(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
            await Assert.That("""{"a":1}""").ContainsJson("{ not json"))
            .Throws<AssertionException>();
        await Assert.That(ex!.Message).Contains("expected JSON to be parseable", StringComparison.Ordinal);
    }

    [Test]
    public async Task Subset_NullArgumentGuards(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(() => "".ContainsJson(null!)).Throws<ArgumentNullException>();
        using var response = JsonResponse("{}");
        await Assert.That(() => response.ContainsJson(null!, ct)).Throws<ArgumentNullException>();
    }
}
