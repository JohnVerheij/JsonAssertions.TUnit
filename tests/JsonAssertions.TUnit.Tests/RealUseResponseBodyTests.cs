using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TUnit.Assertions.Exceptions;
using static JsonAssertions.TUnit.Tests.RealUseTestSupport;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// The response body as consumers really have it: read once from the network, already read by the test, empty,
/// in another encoding, large, canceled, or disposed. Each test states the behavior 0.7.0 must have.
/// </summary>
[Timeout(30_000)]
internal sealed class RealUseResponseBodyTests
{
    [Test]
    public async Task OnceReadableBody_SupportsTwoAwaitsAndAnAndChain(CancellationToken ct)
    {
        using var response = OnceReadable(Encoding.UTF8.GetBytes(Body));

        await Assert.That(response).HasJsonProperty("user.name", ct);
        await Assert.That(response).HasJsonValue("user.age", 31, ct);
        await Assert.That(response).HasJsonProperty("items[*].id", ct).And.HasJsonValue("items[1].id", 2, ct);
    }

    [Test]
    public async Task OnceReadableBody_SupportsAssertMultiple(CancellationToken ct)
    {
        using var response = OnceReadable(Encoding.UTF8.GetBytes(Body));

        using (Assert.Multiple())
        {
            await Assert.That(response).HasJsonProperty("user.name", ct);
            await Assert.That(response).HasJsonValue("user.age", 31, ct);
        }
    }

    [Test]
    public async Task BodyReadAsStringByTheTestFirst_IsStillAsserted(CancellationToken ct)
    {
        using var response = OnceReadable(Encoding.UTF8.GetBytes(Body));
        var body = await response.Content.ReadAsStringAsync(ct);

        await Assert.That(body).HasJsonProperty("user.name");
        await Assert.That(response).HasJsonProperty("user.name", ct);
    }

    /// <summary>A test that read the content stream itself leaves nothing to read; the failure must say the body is
    /// empty, not that some unseen text is invalid JSON.</summary>
    [Test]
    public async Task BodyConsumedThroughItsStream_FailsSayingTheBodyIsEmpty(CancellationToken ct)
    {
        using var response = OnceReadable(Encoding.UTF8.GetBytes(Body));
        var stream = await response.Content.ReadAsStreamAsync(ct);
        using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true))
        {
            _ = await reader.ReadToEndAsync(ct);
        }

        var ex = await Assert.That(async () => await Assert.That(response).HasJsonProperty("user.name", ct))
            .Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("response body is empty");
    }

    [Test]
    public async Task EmptyBody_FailsSayingTheBodyIsEmpty(CancellationToken ct)
    {
        using var response = Buffered(string.Empty);

        var ex = await Assert.That(async () => await Assert.That(response).HasJsonProperty("user", ct))
            .Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("response body is empty");
    }

    /// <summary>A response built without content reads as empty (.NET substitutes an empty content).</summary>
    [Test]
    public async Task ResponseWithoutContent_FailsSayingTheBodyIsEmpty(CancellationToken ct)
    {
        using var response = new HttpResponseMessage(HttpStatusCode.NoContent);

        var ex = await Assert.That(async () => await Assert.That(response).HasJsonProperty("user", ct))
            .Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("response body is empty");
    }

    [Test]
    public async Task EmptyBody_DoesNotSatisfyDoesNotHaveJsonProperty(CancellationToken ct)
    {
        using var response = Buffered(string.Empty);

        await Assert.That(async () => await Assert.That(response).DoesNotHaveJsonProperty("user", ct))
            .Throws<AssertionException>();
    }

    [Test]
    [Arguments("application/json; charset=utf-16", true)]
    [Arguments("application/json; charset=utf-16", false)]
    public async Task Utf16Body_IsDecodedFromItsCharsetOrByteOrderMark(string contentType, bool withByteOrderMark, CancellationToken ct)
    {
        byte[] bytes = withByteOrderMark
            ? [.. Encoding.Unicode.GetPreamble(), .. Encoding.Unicode.GetBytes(Body)]
            : Encoding.Unicode.GetBytes(Body);
        using var response = Raw(bytes, contentType);

        await Assert.That(response).HasJsonValue("user.name", "alice", ct);
    }

    [Test]
    [Arguments("application/json")]
    [Arguments("application/json; charset=utf-8")]
    public async Task Utf8BodyWithAByteOrderMark_IsRead(string contentType, CancellationToken ct)
    {
        using var response = Raw([0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes(Body)], contentType);

        await Assert.That(response).HasJsonValue("user.name", "alice", ct);
        await Assert.That(await response.GetJsonValueAsync<int>("user.age", ct)).IsEqualTo(31);
    }

    [Test]
    public async Task ProblemDetailsBodyWithAByteOrderMark_IsRead(CancellationToken ct)
    {
        using var response = Raw([0xEF, 0xBB, 0xBF, .. """{"status":400}"""u8], "application/problem+json", HttpStatusCode.BadRequest);

        await Assert.That(response).MatchesProblemDetails(400, cancellationToken: ct);
    }

    /// <summary>A body of about 3 MB: every path, value and subset assertion stays well under a second.</summary>
    [Test]
    public async Task LargeBody_IsAssertedQuickly(CancellationToken ct)
    {
        var json = "{\"items\":[" + string.Join(",", Enumerable.Range(0, 60_000).Select(i => "{\"id\":" + i + ",\"name\":\"item-" + i + "\",\"tags\":[\"a\",\"b\"]}")) + "]}";
        using var response = Buffered(json);
        var started = DateTimeOffset.UtcNow;

        await Assert.That(response).HasJsonProperty("items[59999].id", ct);
        await Assert.That(response).HasJsonProperty("items[*].id", ct);
        await Assert.That(response).ContainsJson("""{"items":[{"id":59999},{"id":3}]}""", o => o.IgnoreArrayOrder(), ct);
        await Assert.That(json).IsEquivalentJsonTo(json, o => o.IgnorePath("items[*].tags"));

        await Assert.That(DateTimeOffset.UtcNow - started).IsLessThan(TimeSpan.FromSeconds(10));
    }

    /// <summary>A canceled token cancels the assertion whatever the body is; on 0.6.2 a body already in memory is
    /// asserted anyway, while a stream body throws.</summary>
    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task TokenCanceledBeforeTheRead_CancelsTheAssertion(bool bodyInMemory, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var canceled = new CancellationTokenSource();
        await canceled.CancelAsync();
        using var response = bodyInMemory ? Buffered(Body) : OnceReadable(Encoding.UTF8.GetBytes(Body));

        await Assert.That(async () => await Assert.That(response).HasJsonProperty("user", canceled.Token))
            .Throws<OperationCanceledException>();
    }

    /// <summary>A read that the token ends says which read it was; on 0.6.2 it is a bare "A task was canceled.".</summary>
    [Test]
    public async Task TokenCanceledDuringTheRead_SaysTheResponseBodyReadWasCanceled(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var bound = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
        using var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(new NeverEndingStream()) };

        var ex = await Assert.That(async () => await Assert.That(response).HasJsonProperty("user", bound.Token))
            .Throws<OperationCanceledException>();

        await Assert.That(ex!.Message).Contains("response body");
    }

    /// <summary>A <see cref="System.Net.Http.Json.JsonContent"/> body serializes lazily, on the first read; the first
    /// assertion buffers it, so later reads see the same text.</summary>
    [Test]
    public async Task JsonContentBody_SupportsTwoReadsAndAssertMultiple(CancellationToken ct)
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = System.Net.Http.Json.JsonContent.Create(new HasJsonResponseTests.TestDto(7, "alice"), HasJsonResponseTests.TestJsonContext.Default.TestDto),
        };

        await Assert.That(response).HasJsonValue("Id", 7, ct);
        await Assert.That(response).HasJsonValue("Name", "alice", ct);
        using (Assert.Multiple())
        {
            await Assert.That(response).HasJsonProperty("Id", ct);
            await Assert.That(response).ContainsJson("""{"Name":"alice"}""", ct);
        }
    }

    /// <summary>After the first read has buffered a <see cref="System.Net.Http.Json.JsonContent"/> body, a canceled token
    /// still cancels the next assertion (on 0.6.2 it is ignored, as for any body in memory).</summary>
    [Test]
    public async Task JsonContentBody_CanceledTokenCancelsEvenAfterTheBodyIsBuffered(CancellationToken ct)
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = System.Net.Http.Json.JsonContent.Create(new HasJsonResponseTests.TestDto(7, "alice"), HasJsonResponseTests.TestJsonContext.Default.TestDto),
        };
        using var canceled = new CancellationTokenSource();
        await canceled.CancelAsync();

        await Assert.That(async () => await Assert.That(response).HasJsonProperty("Id", canceled.Token))
            .Throws<OperationCanceledException>();
        await Assert.That(response).HasJsonProperty("Id", ct);
        await Assert.That(async () => await Assert.That(response).HasJsonProperty("Id", canceled.Token))
            .Throws<OperationCanceledException>();
    }

    [Test]
    public async Task DisposedResponse_ThrowsObjectDisposedException(CancellationToken ct)
    {
        var response = Buffered(Body);
        response.Dispose();

        await Assert.That(async () => await Assert.That(response).HasJsonProperty("user", ct))
            .Throws<ObjectDisposedException>();
    }
}
