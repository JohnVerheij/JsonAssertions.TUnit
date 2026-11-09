using System;
using System.Diagnostics.CodeAnalysis;
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
    [SuppressMessage(
        "Major Bug",
        "S8949:Pass the cancellationToken to this method",
        Justification = "The omitted CancellationToken argument is the subject of this test; passing one would stop exercising the optional-parameter path.")]
    public async Task HasJsonProperty_WithoutExplicitCancellationToken_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var response = Response(BodyJson);

        // The CancellationToken parameter is optional; omitting it uses CancellationToken.None.
        await Assert.That(response).HasJsonProperty("user.name");
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

        // A bare int literal binds to the int overload, which matches the JSON number exactly.
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

        await Assert.That(ex!.Message).Contains("Expected to have the JSON string 'bob' at path 'user.name'\nbut found \"alice\"");
    }

    [Test]
    public async Task HasJsonValue_Int64_StringEncodedMatch_Passes(CancellationToken ct)
    {
        using var response = Response("""{"guid":{"high":"123456789012345"}}""");
        await Assert.That(response).HasJsonValue("guid.high", 123456789012345L, ct);
    }

    [Test]
    public async Task HasJsonValue_UInt64_StringEncodedMatch_Passes(CancellationToken ct)
    {
        using var response = Response("""{"guid":{"low":"18446744073709551615"}}""");
        await Assert.That(response).HasJsonValue("guid.low", ulong.MaxValue, ct);
    }

    [Test]
    public async Task HasJsonValue_Int64_NumberEncodedMatch_Passes(CancellationToken ct)
    {
        using var response = Response("""{"guid":{"high":123456789012345}}""");
        await Assert.That(response).HasJsonValue("guid.high", 123456789012345L, ct);
    }

    [Test]
    public async Task HasJsonValue_Int32_StringEncodedMatch_Passes(CancellationToken ct)
    {
        using var response = Response("""{"code":"200"}""");
        await Assert.That(response).HasJsonValue("code", 200, ct);
    }

    [Test]
    public async Task HasJsonValueOneOf_Int64_Match_Passes(CancellationToken ct)
    {
        using var response = Response("""{"seq":"200"}""");
        await Assert.That(response).HasJsonValueOneOf("seq", [100L, 200L], ct);
    }

    [Test]
    public async Task HasJsonValueOneOf_UInt64_Match_Passes(CancellationToken ct)
    {
        using var response = Response("""{"seq":"7"}""");
        await Assert.That(response).HasJsonValueOneOf("seq", [7UL, 8UL], ct);
    }

    [Test]
    public async Task HasJsonValue_Int32_NumberMatch_Passes(CancellationToken ct)
    {
        using var response = Response("""{"code":200}""");
        await Assert.That(response).HasJsonValue("code", 200, ct);
    }

    [Test]
    public async Task HasJsonValue_UInt32_NumberMatch_Passes(CancellationToken ct)
    {
        using var response = Response("""{"code":4294967295}""");
        await Assert.That(response).HasJsonValue("code", uint.MaxValue, ct);
    }

    [Test]
    public async Task HasJsonValueOneOf_Int32_Match_Passes(CancellationToken ct)
    {
        using var response = Response("""{"code":404}""");
        await Assert.That(response).HasJsonValueOneOf("code", [200, 404], ct);
    }

    [Test]
    public async Task HasJsonValueOneOf_UInt32_Match_Passes(CancellationToken ct)
    {
        using var response = Response("""{"code":7}""");
        await Assert.That(response).HasJsonValueOneOf("code", [7U, 8U], ct);
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

        await Assert.That(ex!.Message).Contains("but the value is not valid JSON: ");
    }

    [Test]
    public async Task HasNonEmptyJsonString_Match_Passes(CancellationToken ct)
    {
        using var response = Response(BodyJson);
        await Assert.That(response).HasNonEmptyJsonString("user.name", ct);
    }

    [Test]
    public async Task HasJsonBoolean_Match_Passes(CancellationToken ct)
    {
        using var response = Response(BodyJson);
        await Assert.That(response).HasJsonBoolean("user.active", ct);
    }

    [Test]
    public async Task HasJsonValueMatching_PredicateSatisfied_Passes(CancellationToken ct)
    {
        using var response = Response(BodyJson);
        await Assert.That(response).HasJsonValueMatching(
            "user.age",
            static element => element.GetInt32() > 18,
            ct);
    }

    [Test]
    public async Task HasJsonValueOneOf_String_Match_Passes(CancellationToken ct)
    {
        using var response = Response(BodyJson);
        await Assert.That(response).HasJsonValueOneOf("user.name", ["alice", "bob", "carol"], ct);
    }

    [Test]
    public async Task HasJsonValueOneOf_Number_Match_Passes(CancellationToken ct)
    {
        using var response = Response(BodyJson);
        await Assert.That(response).HasJsonValueOneOf("user.age", [25.0, 30.0, 35.0], ct);
    }

    [Test]
    public async Task HasJsonValueParsableAs_Guid_Match_Passes(CancellationToken ct)
    {
        using var response = Response("""{"id":"550e8400-e29b-41d4-a716-446655440000"}""");
        await Assert.That(response).HasJsonValueParsableAs<Guid>("id", ct);
    }

    [Test]
    public async Task HasJsonValueParsableAs_NotParsable_Fails(CancellationToken ct)
    {
        using var response = Response(BodyJson);

        await Assert.That(async () => await Assert.That(response).HasJsonValueParsableAs<Guid>("user.name", ct))
            .Throws<AssertionException>();
    }

    [Test]
    public async Task HasJsonValue_Double_Match_Passes(CancellationToken ct)
    {
        using var response = Response("""{"ratio":2.5}""");
        await Assert.That(response).HasJsonValue("ratio", 2.5, ct);
    }
}
