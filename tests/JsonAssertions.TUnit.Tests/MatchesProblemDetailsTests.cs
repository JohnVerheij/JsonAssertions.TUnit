using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TUnit.Assertions.Exceptions;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// End-to-end tests for <c>MatchesProblemDetails</c> and <c>MatchesValidationProblemDetails</c>:
/// the assertion path uses the internal <see cref="JsonAssertions.ProblemDetailsMirror"/>
/// (production package stays MIT-clean and AOT-clean, no <c>Microsoft.AspNetCore.Mvc.Abstractions</c>
/// dep), but the TESTS exercise the mirror against the REAL
/// <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> wire-format (test project takes the
/// <c>Microsoft.AspNetCore.App</c> framework reference; Apache 2.0 is acceptable in test code
/// per the family's "shipped code is MIT only; test code can use other licenses" rule).
/// </summary>
/// <remarks>
/// The mirror-vs-real comparison is the test's reason to exist: if ASP.NET Core changes the
/// ProblemDetails wire format in a future release, this test catches the drift at CI time
/// rather than at consumer runtime.
/// </remarks>
[Category("Smoke")]
[Timeout(5_000)]
internal sealed class MatchesProblemDetailsTests
{
    private static HttpResponseMessage ProblemResponse(HttpStatusCode status, string body)
    {
        var response = new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8),
        };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/problem+json");
        return response;
    }

    private static readonly JsonSerializerOptions RealSerializeOptions = new()
    {
        // Match ASP.NET Core's default ProblemDetails serialization shape.
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static string SerializeReal(object value)
        => JsonSerializer.Serialize(value, RealSerializeOptions);

    [Test]
    public async Task MatchesProblemDetails_AllFieldsMatch_PassesAgainstRealProblemDetails(CancellationToken ct)
    {
        var real = new ProblemDetails
        {
            Type = "https://example.com/probs/validation",
            Title = "Validation failed",
            Status = 400,
            Detail = "Field X is required",
            Instance = "/orders/42",
        };
        using var response = ProblemResponse(HttpStatusCode.BadRequest, SerializeReal(real));

        await Assert.That(response).MatchesProblemDetails(
            status: 400,
            title: "Validation failed",
            detail: "Field X is required",
            type: "https://example.com/probs/validation",
            instance: "/orders/42",
            cancellationToken: ct);
    }

    [Test]
    public async Task MatchesProblemDetails_OnlyStatusSpecified_PassesIfStatusMatches(CancellationToken ct)
    {
        var real = new ProblemDetails { Status = 404, Title = "Not found" };
        using var response = ProblemResponse(HttpStatusCode.NotFound, SerializeReal(real));

        await Assert.That(response).MatchesProblemDetails(status: 404, cancellationToken: ct);
    }

    [Test]
    public async Task MatchesProblemDetails_ContentTypeNotProblemJson_Fails(CancellationToken ct)
    {
        // Build a response with status 400 + ProblemDetails body but the WRONG Content-Type.
        var real = new ProblemDetails { Status = 400, Title = "Bad request" };
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(SerializeReal(real), Encoding.UTF8, "application/json"),
        };

        var ex = await Assert.That(async () =>
        {
            await Assert.That(response).MatchesProblemDetails(status: 400, cancellationToken: ct);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("application/problem+json");
        await Assert.That(ex.Message).Contains("application/json");
        response.Dispose();
    }

    [Test]
    public async Task MatchesProblemDetails_StatusMismatch_FailsWithFieldDiagnostic(CancellationToken ct)
    {
        var real = new ProblemDetails { Status = 500, Title = "Server error" };
        using var response = ProblemResponse(HttpStatusCode.InternalServerError, SerializeReal(real));

        var ex = await Assert.That(async () =>
        {
            await Assert.That(response).MatchesProblemDetails(status: 400, cancellationToken: ct);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("status: expected 400");
        await Assert.That(ex.Message).Contains("got 500");
    }

    private static readonly string[] FieldXMessages = ["is required"];
    private static readonly string[] FieldYMessages = ["must be positive"];

    [Test]
    public async Task MatchesValidationProblemDetails_ErrorsMatch_Passes(CancellationToken ct)
    {
        var real = new ValidationProblemDetails(new Dictionary<string, string[]>(System.StringComparer.Ordinal)
        {
            ["FieldX"] = FieldXMessages,
            ["FieldY"] = FieldYMessages,
        })
        {
            Status = 400,
            Title = "Validation failed",
        };
        using var response = ProblemResponse(HttpStatusCode.BadRequest, SerializeReal(real));

        var expectedErrors = new Dictionary<string, string[]>(System.StringComparer.Ordinal)
        {
            ["FieldX"] = FieldXMessages,
            ["FieldY"] = FieldYMessages,
        };

        await Assert.That(response).MatchesValidationProblemDetails(
            status: 400,
            errors: expectedErrors,
            cancellationToken: ct);
    }

    [Test]
    public async Task MatchesValidationProblemDetails_MissingErrorField_Fails(CancellationToken ct)
    {
        var real = new ValidationProblemDetails(new Dictionary<string, string[]>(System.StringComparer.Ordinal)
        {
            ["FieldX"] = FieldXMessages,
        })
        {
            Status = 400,
        };
        using var response = ProblemResponse(HttpStatusCode.BadRequest, SerializeReal(real));

        var expectedErrors = new Dictionary<string, string[]>(System.StringComparer.Ordinal)
        {
            ["FieldX"] = FieldXMessages,
            ["FieldY"] = FieldYMessages,  // not present in actual
        };

        var ex = await Assert.That(async () =>
        {
            await Assert.That(response).MatchesValidationProblemDetails(
                status: 400, errors: expectedErrors, cancellationToken: ct);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("FieldY");
    }
}
