using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using TUnit.Assertions.Exceptions;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// End-to-end tests for the <c>HasJsonResponse&lt;T&gt;</c> entry point: a combined HTTP
/// status + AOT-clean deserialization + structural-equality assertion against an
/// <see cref="HttpResponseMessage"/>. Covers the success path, status mismatch (with body
/// preserved in the diagnostic), malformed-JSON deserialization failure, and a structural
/// value mismatch. The supplied <c>JsonTypeInfo&lt;T&gt;</c> is the source-generated entry
/// from <see cref="TestJsonContext"/>, so the assertion path is AOT-clean.
/// </summary>
[Category("Smoke")]
[Timeout(5_000)]
internal sealed partial class HasJsonResponseTests
{
    private static HttpResponseMessage Response(HttpStatusCode status, string body)
        => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    [Test]
    public async Task HasJsonResponse_StatusAndValueMatch_Passes(CancellationToken ct)
    {
        using var response = Response(HttpStatusCode.OK, """{"Id":42,"Name":"alice"}""");
        var expected = new TestDto(42, "alice");

        await Assert.That(response).HasJsonResponse(HttpStatusCode.OK, TestJsonContext.Default.TestDto, expected, ct);
    }

    [Test]
    public async Task HasJsonResponse_StatusMismatch_FailsWithBodyInMessage(CancellationToken ct)
    {
        using var response = Response(HttpStatusCode.BadRequest, """{"error":"validation failed"}""");
        var expected = new TestDto(0, string.Empty);

        var ex = await Assert.That(async () =>
        {
            await Assert.That(response).HasJsonResponse(HttpStatusCode.OK, TestJsonContext.Default.TestDto, expected, ct);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("Expected to have status OK and a JSON body equal to TestDto");
        await Assert.That(ex.Message).Contains("but the status was 400 BadRequest; body: ");
        await Assert.That(ex.Message).Contains("validation failed");
    }

    /// <summary>A status mismatch on a response with no body names the status alone.</summary>
    [Test]
    public async Task HasJsonResponse_StatusMismatchWithoutABody_OmitsTheBody(CancellationToken ct)
    {
        using var response = new HttpResponseMessage(HttpStatusCode.NoContent);

        var ex = await Assert.That(async () => await Assert.That(response).HasJsonResponse(HttpStatusCode.OK, TestJsonContext.Default.TestDto, new TestDto(1, "a"), ct))
            .Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("but the status was 204 NoContent\n\nat Assert.That(response)");
    }

    [Test]
    public async Task HasJsonResponse_MalformedJson_FailsWithExplainedMessage(CancellationToken ct)
    {
        using var response = Response(HttpStatusCode.OK, "{ not valid json");
        var expected = new TestDto(42, "alice");

        var ex = await Assert.That(async () =>
        {
            await Assert.That(response).HasJsonResponse(HttpStatusCode.OK, TestJsonContext.Default.TestDto, expected, ct);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("TestDto");
        await Assert.That(ex.Message).Contains("but the body did not deserialize as TestDto: ");
    }

    [Test]
    public async Task HasJsonResponse_ValueMismatch_FailsWithStructuralDiagnostic(CancellationToken ct)
    {
        using var response = Response(HttpStatusCode.OK, """{"Id":99,"Name":"bob"}""");
        var expected = new TestDto(42, "alice");

        var ex = await Assert.That(async () =>
        {
            await Assert.That(response).HasJsonResponse(HttpStatusCode.OK, TestJsonContext.Default.TestDto, expected, ct);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("TestDto");
        await Assert.That(ex.Message).Contains("but the body deserialized to TestDto { Id = 99, Name = bob }");
    }

    [Test]
    public async Task HasJsonResponse_BodyIsLiteralJsonNull_FailsWithNullActualDiagnostic(CancellationToken ct)
    {
        // Body deserializes to null; actual?.ToString() ?? "null" walks the null-coalescing
        // "null" branch on the actual side.
        using var response = Response(HttpStatusCode.OK, "null");
        var expected = new TestDto(42, "alice");

        var ex = await Assert.That(async () =>
        {
            await Assert.That(response).HasJsonResponse(HttpStatusCode.OK, TestJsonContext.Default.TestDto, expected, ct);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("null");
    }

    [Test]
    public async Task HasJsonResponse_LongMalformedBody_FailsWithTruncatedDiagnostic(CancellationToken ct)
    {
        // Response body > 256 chars; the failure message must truncate via TruncateBody's
        // body > MaxResponseBodyLength branch.
        var longInvalid = "{ " + new string('x', 300);
        using var response = Response(HttpStatusCode.OK, longInvalid);
        var expected = new TestDto(42, "alice");

        var ex = await Assert.That(async () =>
        {
            await Assert.That(response).HasJsonResponse(HttpStatusCode.OK, TestJsonContext.Default.TestDto, expected, ct);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("...");
        await Assert.That(ex.Message).Contains("but the body did not deserialize as TestDto: ");
    }

    [Test]
    [SuppressMessage(
        "Major Bug",
        "S8949:Pass the cancellationToken to this method",
        Justification = "The omitted CancellationToken argument is the subject of this test; passing one would stop exercising the optional-parameter path.")]
    public async Task HasJsonResponse_WithoutExplicitCancellationToken_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var response = Response(HttpStatusCode.OK, """{"Id":42,"Name":"alice"}""");
        var expected = new TestDto(42, "alice");

        // The CancellationToken parameter is optional; omitting it uses CancellationToken.None.
        await Assert.That(response).HasJsonResponse(HttpStatusCode.OK, TestJsonContext.Default.TestDto, expected);
    }

    /// <summary>A minimal record used as the deserialization target. Records get value-equality
    /// for free, which is what <c>HasJsonResponse</c>'s structural comparison relies on.</summary>
    internal sealed record TestDto(int Id, string Name);

    /// <summary>STJ source-gen context for <see cref="TestDto"/>. The outer
    /// <see cref="HasJsonResponseTests"/> class is partial so the source generator can emit
    /// the generated half of this context next to its declaration.</summary>
    [JsonSerializable(typeof(TestDto))]
    internal sealed partial class TestJsonContext : JsonSerializerContext;
}
