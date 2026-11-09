using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static JsonAssertions.TUnit.Tests.RealUseTestSupport;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// The same string, the same response and the same options used from 8 threads at once, the way parallel tests
/// share a fixture.
/// </summary>
[Timeout(60_000)]
internal sealed class RealUseConcurrencyTests
{
    private const int Threads = 8;

    [Test]
    public async Task SameString_AssertedFromEightThreads(CancellationToken ct)
    {
        await RunOnThreadsAsync(200, async () =>
        {
            await Assert.That(Body).HasJsonProperty("items[*].id");
            await Assert.That(Body).ContainsJson("""{"user":{"name":"alice"}}""");
        }, ct);
    }

    /// <summary>A response from <see cref="HttpClient"/> with the default completion option is buffered, so parallel
    /// assertions read the same buffer. (An unbuffered response, from <see cref="HttpCompletionOption.ResponseHeadersRead"/>,
    /// cannot be read by parallel first reads: .NET throws "The stream was already consumed"; see the README.)</summary>
    [Test]
    public async Task SameBufferedResponse_AssertedFromEightThreads(CancellationToken ct)
    {
        using var handler = new FixedResponseHandler(() => new StreamContent(new OnceReadableStream(Encoding.UTF8.GetBytes(Body))));
        using var client = new HttpClient(handler);
        using var response = await client.GetAsync(new UriBuilder(Uri.UriSchemeHttp, "localhost").Uri, ct);

        await RunOnThreadsAsync(200, async () =>
        {
            await Assert.That(response).HasJsonProperty("user.name", ct);
            await Assert.That(response).HasJsonValue("items[1].id", 2, ct);
        }, ct);
    }

    [Test]
    public async Task SharedEquivalenceOptions_UsedFromEightThreads(CancellationToken ct)
    {
        var shared = new JsonEquivalenceOptions().IgnorePath("items[*].tags").IgnoreArrayOrder();
        using var expected = JsonDocument.Parse("""{"x":1,"items":[{"id":1,"tags":["a"]},{"id":2}]}""");
        using var actual = JsonDocument.Parse("""{"items":[{"id":2},{"id":1,"tags":["b"]}],"x":1}""");
        var expectedRoot = expected.RootElement;
        var actualRoot = actual.RootElement;

        await RunOnThreadsAsync(2_000, async () =>
        {
            await Assert.That(JsonEquivalence.Compare(expectedRoot, actualRoot, shared)).IsNull();
        }, ct);
    }

    private static Task RunOnThreadsAsync(int iterations, Func<Task> body, CancellationToken ct)
        => Task.WhenAll(Enumerable.Range(0, Threads).Select(_ => Task.Run(async () =>
        {
            for (var i = 0; i < iterations; i++)
            {
                await body();
            }
        }, ct)));

    private sealed class FixedResponseHandler(Func<HttpContent> content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content(), RequestMessage = request });
    }
}
