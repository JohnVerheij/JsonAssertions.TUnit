using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TUnit.Assertions.Exceptions;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// End-to-end tests for the <c>[GenerateAssertion]</c>-emitted <see cref="HttpResponseMessage"/>
/// chains: the response body is read as text and the property / value / shape assertions run
/// against it. Covers the happy path for each entry point, a representative value mismatch,
/// and a malformed-body response (which must fail with an explained message, not throw).
/// </summary>
[Category("Smoke")]
[Timeout(5_000)]
internal sealed class HttpResponseMessageAssertionsTests
{
    private const string BodyJson =
        """{"user":{"name":"alice","age":30,"active":true},"items":[1,2,3]}""";

    private static HttpResponseMessage Response(string body)
        => new() { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    [Test]
    public async Task HasJsonProperty_Present_Passes(CancellationToken ct)
    {
        using var response = Response(BodyJson);
        await Assert.That(response).HasJsonProperty("user.name", ct);
    }

    [Test]
    public async Task DoesNotHaveJsonProperty_Absent_Passes(CancellationToken ct)
    {
        using var response = Response(BodyJson);
        await Assert.That(response).DoesNotHaveJsonProperty("user.email", ct);
    }

    [Test]
    public async Task HasJsonValue_String_Match_Passes(CancellationToken ct)
    {
        using var response = Response(BodyJson);
        await Assert.That(response).HasJsonValue("user.name", "alice", ct);
    }

    [Test]
    public async Task HasJsonValue_Boolean_Match_Passes(CancellationToken ct)
    {
        using var response = Response(BodyJson);
        await Assert.That(response).HasJsonValue("user.active", true, ct);
    }

    [Test]
    public async Task HasJsonValue_Number_Match_Passes(CancellationToken ct)
    {
        using var response = Response(BodyJson);
        await Assert.That(response).HasJsonValue("user.age", 30, ct);
    }

    [Test]
    public async Task HasJsonValue_Mismatch_Fails(CancellationToken ct)
    {
        using var response = Response(BodyJson);
        var ex = await Assert.That(async () =>
        {
            await Assert.That(response).HasJsonValue("user.name", "bob", ct);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("found: \"alice\"");
    }

    [Test]
    public async Task HasJsonArrayLength_Match_Passes(CancellationToken ct)
    {
        using var response = Response(BodyJson);
        await Assert.That(response).HasJsonArrayLength("items", 3, ct);
    }

    [Test]
    public async Task HasNonEmptyJsonArray_Match_Passes(CancellationToken ct)
    {
        using var response = Response(BodyJson);
        await Assert.That(response).HasNonEmptyJsonArray("items", ct);
    }

    [Test]
    public async Task HasEmptyJsonArray_NonEmpty_Fails(CancellationToken ct)
    {
        using var response = Response(BodyJson);
        var ex = await Assert.That(async () =>
        {
            await Assert.That(response).HasEmptyJsonArray("items", ct);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("to have an empty JSON array");
    }

    [Test]
    public async Task HasJsonValueKind_Match_Passes(CancellationToken ct)
    {
        using var response = Response(BodyJson);
        await Assert.That(response).HasJsonValueKind("items", JsonValueKind.Array, ct);
    }

    [Test]
    public async Task HasJsonProperty_MalformedBody_FailsWithParseMessage(CancellationToken ct)
    {
        using var response = Response("{ not json");
        var ex = await Assert.That(async () =>
        {
            await Assert.That(response).HasJsonProperty("user.name", ct);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("parseable JSON");
        await Assert.That(ex.Message).Contains("but parsing failed:");
    }
}
