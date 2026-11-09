using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TUnit.Assertions.Exceptions;
using static JsonAssertions.TUnit.Tests.RealUseTestSupport;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// Every reader of a response body, assertion or extraction, reads it the same way: an empty body says so, a canceled
/// token cancels the call, and a read that the token cancels says which read it was.
/// </summary>
[Timeout(30_000)]
internal sealed class ResponseBodyReadTests
{
    [Test]
    public async Task Extraction_EmptyBody_ThrowsSayingTheBodyIsEmpty(CancellationToken ct)
    {
        using var response = Buffered(string.Empty);

        var value = await Assert.That(async () => await response.GetJsonValueAsync<int>("user.age", ct))
            .Throws<JsonExtractionException>();
        var text = await Assert.That(async () => await response.GetJsonStringAsync("user.name", ct))
            .Throws<JsonExtractionException>();
        var element = await Assert.That(async () => await response.GetJsonElementAsync("user", ct))
            .Throws<JsonExtractionException>();
        var values = await Assert.That(async () => await response.GetJsonValuesAsync<int>("items[*].id", ct))
            .Throws<JsonExtractionException>();

        await Assert.That(value!.Message).StartsWith("The response body is empty; ");
        await Assert.That(text!.Message).StartsWith("The response body is empty; ");
        await Assert.That(element!.Message).StartsWith("The response body is empty; ");
        await Assert.That(values!.Message).StartsWith("The response body is empty; ");
    }

    [Test]
    public async Task Extraction_CanceledToken_CancelsAlsoForABodyInMemory(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var canceled = new CancellationTokenSource();
        await canceled.CancelAsync();
        using var response = Buffered(Body);

        await Assert.That(async () => await response.GetJsonValueAsync<int>("user.age", canceled.Token))
            .Throws<OperationCanceledException>();
        await Assert.That(async () => await response.GetJsonStringAsync("user.name", canceled.Token))
            .Throws<OperationCanceledException>();
        await Assert.That(async () => await response.GetJsonElementAsync("user", canceled.Token))
            .Throws<OperationCanceledException>();
        await Assert.That(async () => await response.GetJsonValuesAsync<int>("items[*].id", canceled.Token))
            .Throws<OperationCanceledException>();
    }

    [Test]
    public async Task ReadCanceledByTheToken_SaysWhichReadAndCarriesTheToken(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var assertionBound = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        using var assertionResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new NeverEndingStream()) };
        using var extractionResponse = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new NeverEndingStream()) };

        var assertion = await Assert.That(async () => await Assert.That(assertionResponse).HasJsonProperty("user", assertionBound.Token))
            .Throws<OperationCanceledException>();
        using var extractionBound = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        var extraction = await Assert.That(async () => await extractionResponse.GetJsonStringAsync("user", extractionBound.Token))
            .Throws<OperationCanceledException>();

        await Assert.That(assertion!.Message).IsEqualTo("The response body read was canceled before the JSON assertion ran.");
        await Assert.That(assertion.CancellationToken).IsEqualTo(assertionBound.Token);
        await Assert.That(assertion.InnerException).IsNotNull();
        await Assert.That(extraction!.Message).IsEqualTo("The response body read was canceled before the JSON value was read.");
        await Assert.That(extraction.CancellationToken).IsEqualTo(extractionBound.Token);
    }

    /// <summary>A cancellation that does not come from the assertion's token is not relabeled as one.</summary>
    [Test]
    public async Task ReadCanceledByAnotherSource_PropagatesUnchanged(CancellationToken ct)
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ForeignCanceledContent() };

        var ex = await Assert.That(async () => await Assert.That(response).HasJsonProperty("user", ct))
            .Throws<OperationCanceledException>();

        await Assert.That(ex!.Message).IsEqualTo(ForeignCanceledContent.Message);
    }

    [Test]
    public async Task HasJsonResponse_EmptyBodyWithTheExpectedStatus_FailsSayingTheBodyIsEmpty(CancellationToken ct)
    {
        using var response = Buffered(string.Empty);

        var ex = await Assert.That(async () => await Assert.That(response).HasJsonResponse(
                HttpStatusCode.OK, HasJsonResponseTests.TestJsonContext.Default.TestDto, new HasJsonResponseTests.TestDto(7, "alice"), ct))
            .Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("but the response body is empty");
    }

    [Test]
    public async Task MatchesValidationProblemDetails_EmptyBody_FailsSayingTheBodyIsEmpty(CancellationToken ct)
    {
        using var response = Buffered(string.Empty, HttpStatusCode.BadRequest, "application/problem+json");
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal) { ["Name"] = ["required"] };

        var ex = await Assert.That(async () => await Assert.That(response).MatchesValidationProblemDetails(400, errors, cancellationToken: ct))
            .Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("but the response body is empty");
    }

    /// <summary>No expected errors and no <c>errors</c> in the body: nothing differs.</summary>
    [Test]
    public async Task MatchesValidationProblemDetails_NoErrorsExpectedAndNoneInTheBody_Passes(CancellationToken ct)
    {
        using var response = Buffered("""{"status":400}""", HttpStatusCode.BadRequest, "application/problem+json");

        await Assert.That(response).MatchesValidationProblemDetails(400, new Dictionary<string, string[]>(StringComparer.Ordinal), cancellationToken: ct);
    }

    /// <summary>A content whose serialization is canceled by something other than the caller's token.</summary>
    private sealed class ForeignCanceledContent : HttpContent
    {
        internal const string Message = "canceled by another token";

        /// <inheritdoc/>
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
            => Task.FromException(new OperationCanceledException(Message));

        /// <inheritdoc/>
        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }
}
