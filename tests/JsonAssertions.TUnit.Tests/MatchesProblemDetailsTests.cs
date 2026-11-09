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

    /// <summary><c>status</c> is checked against both the HTTP status code and the body's field (RFC 7807 section
    /// 3.1): on 0.6.2 an HTTP 200 response whose body says 400 passed <c>MatchesProblemDetails(status: 400)</c>.</summary>
    [Test]
    public async Task MatchesProblemDetails_Status_ChecksTheHttpStatusAndTheBodyField(CancellationToken ct)
    {
        using var httpSays200 = ProblemResponse(HttpStatusCode.OK, SerializeReal(new ProblemDetails { Status = 400 }));
        using var bodySays200 = ProblemResponse(HttpStatusCode.BadRequest, SerializeReal(new ProblemDetails { Status = 200 }));
        using var httpSays299 = ProblemResponse((HttpStatusCode)299, SerializeReal(new ProblemDetails { Status = 400 }));

        var http = await Assert.That(async () => await Assert.That(httpSays200).MatchesProblemDetails(status: 400, cancellationToken: ct))
            .Throws<AssertionException>();
        var body = await Assert.That(async () => await Assert.That(bodySays200).MatchesProblemDetails(status: 400, cancellationToken: ct))
            .Throws<AssertionException>();
        var unnamed = await Assert.That(async () => await Assert.That(httpSays299).MatchesProblemDetails(status: 400, cancellationToken: ct))
            .Throws<AssertionException>();

        await Assert.That(http!.Message).Contains("but 1 field differed:\n  HTTP status: 200 OK (expected 400)");
        await Assert.That(body!.Message).Contains("but 1 field differed:\n  status: 200 (expected 400)");
        await Assert.That(unnamed!.Message).Contains("HTTP status: 299 (expected 400)");
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

        await Assert.That(ex!.Message).Contains(
            "but 2 fields differed:\n  HTTP status: 500 InternalServerError (expected 400)\n  status: 500 (expected 400)");
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
    public async Task MatchesProblemDetails_ExtensionMembers_CapturedAndIgnoredByCurrentAssertion(CancellationToken ct)
    {
        // RFC 7807 §3.2 extension members: ProblemDetails responses can carry custom fields
        // beyond the standard type/title/status/detail/instance set. The mirror captures
        // these into the Extensions dictionary via [JsonExtensionData] so the deserialization
        // is loss-free; the assertion path doesn't yet expose a WithExtension(...) chain method
        // (deferred to v0.3.1 / v0.4.0), so unknown fields don't break the current shape match.
        const string bodyWithExtensions = """
            {"type":"https://example.com/probs/x","title":"Test","status":400,"detail":"d",
             "traceId":"abc123","validationStage":"model-binding"}
            """;
        using var response = ProblemResponse(HttpStatusCode.BadRequest, bodyWithExtensions);

        // Asserts status only. The extension members (traceId, validationStage) are captured
        // by the mirror's Extensions dictionary on deserialization but the current public
        // surface does not assert on them, so they do not affect this pass.
        await Assert.That(response).MatchesProblemDetails(status: 400, cancellationToken: ct);
    }

    [Test]
    public async Task MatchesProblemDetails_BodyIsLiteralJsonNull_FailsWithFieldDiagnostic(CancellationToken ct)
    {
        // Body deserializes to a null ProblemDetailsMirror; the assertion still walks the field
        // comparisons (every mirror?.X check exercises the null-mirror branch) and reports the
        // status mismatch.
        using var response = ProblemResponse(HttpStatusCode.BadRequest, "null");

        var ex = await Assert.That(async () =>
        {
            await Assert.That(response).MatchesProblemDetails(
                status: 400,
                title: "expected title",
                detail: "expected detail",
                type: "expected-type",
                instance: "/expected",
                cancellationToken: ct);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("status");
    }

    [Test]
    public async Task MatchesProblemDetails_NoContentTypeHeader_FailsWithContentTypeDiagnostic(CancellationToken ct)
    {
        // Build a response with NO Content-Type header at all; the assertion's null-conditional
        // on Headers.ContentType?.MediaType walks the null branch.
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"status":400}""", Encoding.UTF8),
        };
        // Strip the auto-set Content-Type that StringContent adds.
        response.Content.Headers.ContentType = null;

        var ex = await Assert.That(async () =>
        {
            await Assert.That(response).MatchesProblemDetails(status: 400, cancellationToken: ct);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("application/problem+json");
        response.Dispose();
    }

    [Test]
    public async Task MatchesProblemDetails_EmptyBody_FailsSayingTheBodyIsEmpty(CancellationToken ct)
    {
        // Empty body + correct Content-Type: the same text as every other response reader, not a parse failure.
        using var response = ProblemResponse(HttpStatusCode.BadRequest, string.Empty);

        var ex = await Assert.That(async () =>
        {
            await Assert.That(response).MatchesProblemDetails(status: 400, cancellationToken: ct);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("but the response body is empty");
    }

    [Test]
    public async Task MatchesProblemDetails_MixedCaseContentType_Passes(CancellationToken ct)
    {
        // HTTP RFC 9110 §8.3.2: media-type tokens are case-insensitive. A response with
        // Application/Problem+Json must satisfy the RFC 7807 content-type check.
        var real = new ProblemDetails { Status = 400, Title = "Bad request" };
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(SerializeReal(real), Encoding.UTF8),
        };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue("Application/Problem+Json");

        await Assert.That(response).MatchesProblemDetails(status: 400, cancellationToken: ct);
        response.Dispose();
    }

    [Test]
    public async Task MatchesProblemDetails_BodyNotValidJson_FailsWithParseDiagnostic(CancellationToken ct)
    {
        // Body has the correct content-type but is not valid JSON; the assertion must surface
        // the parse failure via ProblemDetailsParseFailed.
        using var response = ProblemResponse(HttpStatusCode.BadRequest, "{not-json");

        var ex = await Assert.That(async () =>
        {
            await Assert.That(response).MatchesProblemDetails(status: 400, cancellationToken: ct);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("but the body did not deserialize as ProblemDetails: ");
    }

    [Test]
    public async Task MatchesProblemDetails_TitleAndDetailMismatch_FailsWithBothFields(CancellationToken ct)
    {
        // Multiple non-matching fields are reported in a single failure message.
        var real = new ProblemDetails
        {
            Status = 400,
            Title = "Actual title",
            Detail = "Actual detail",
        };
        using var response = ProblemResponse(HttpStatusCode.BadRequest, SerializeReal(real));

        var ex = await Assert.That(async () =>
        {
            await Assert.That(response).MatchesProblemDetails(
                status: 400,
                title: "Expected title",
                detail: "Expected detail",
                cancellationToken: ct);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("title");
        await Assert.That(ex.Message).Contains("detail");
        await Assert.That(ex.Message).Contains("Expected title");
        await Assert.That(ex.Message).Contains("Expected detail");
    }

    [Test]
    public async Task MatchesProblemDetails_TypeAndInstanceMismatch_FailsWithBothFields(CancellationToken ct)
    {
        var real = new ProblemDetails
        {
            Status = 400,
            Type = "actual-type",
            Instance = "/actual/instance",
        };
        using var response = ProblemResponse(HttpStatusCode.BadRequest, SerializeReal(real));

        var ex = await Assert.That(async () =>
        {
            await Assert.That(response).MatchesProblemDetails(
                status: 400,
                type: "expected-type",
                instance: "/expected/instance",
                cancellationToken: ct);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("type");
        await Assert.That(ex.Message).Contains("instance");
    }

    [Test]
    public async Task MatchesValidationProblemDetails_NoErrorsProperty_FailsExplicitly(CancellationToken ct)
    {
        // Response body is plain ProblemDetails (no "errors" property at all); the list says so in one entry.
        var real = new ProblemDetails { Status = 400, Title = "No errors here" };
        using var response = ProblemResponse(HttpStatusCode.BadRequest, SerializeReal(real));

        var expectedErrors = new Dictionary<string, string[]>(System.StringComparer.Ordinal)
        {
            ["FieldX"] = FieldXMessages,
        };

        var ex = await Assert.That(async () =>
        {
            await Assert.That(response).MatchesValidationProblemDetails(
                status: 400, errors: expectedErrors, cancellationToken: ct);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("but 1 entry differed:\n  errors: absent");
    }

    [Test]
    public async Task MatchesValidationProblemDetails_NullMessageArray_FailsAndRendersNull(CancellationToken ct)
    {
        // "errors": { "FieldX": null } deserializes to a null message array (nullable annotations are not
        // applied to dictionary values). It must not match an expected empty array, and the diagnostic
        // renders it instead of throwing.
        using var response = ProblemResponse(HttpStatusCode.BadRequest, """{"status":400,"errors":{"FieldX":null}}""");
        var expectedErrors = new Dictionary<string, string[]>(System.StringComparer.Ordinal)
        {
            ["FieldX"] = [],
        };

        var ex = await Assert.That(async () =>
        {
            await Assert.That(response).MatchesValidationProblemDetails(
                status: 400, errors: expectedErrors, cancellationToken: ct);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("errors.FieldX: null (expected [])");
    }

    [Test]
    public async Task MatchesValidationProblemDetails_NullExpectedMessageArray_ThrowsArgumentException(CancellationToken ct)
    {
        // A null expected message array (for example from a fixture deserialized with "FieldX": null) is not
        // an expectation: it throws instead of matching the empty array in the body, and it throws before the
        // response is compared, so a body without the field throws too.
        using var response = ProblemResponse(HttpStatusCode.BadRequest, """{"status":400,"errors":{"FieldX":[]}}""");
        using var responseWithoutField = ProblemResponse(HttpStatusCode.BadRequest, """{"status":400,"errors":{}}""");
        var expectedErrors = new Dictionary<string, string[]>(System.StringComparer.Ordinal)
        {
            ["FieldX"] = null!,
        };

        var ex = await Assert.That(async () =>
        {
            await Assert.That(response).MatchesValidationProblemDetails(
                status: 400, errors: expectedErrors, cancellationToken: ct);
        }).Throws<System.ArgumentException>();

        await Assert.That(ex!.Message).Contains("\"FieldX\" is null");
        await Assert.That(ex.ParamName).IsEqualTo("errors");

        await Assert.That(async () =>
        {
            await Assert.That(responseWithoutField).MatchesValidationProblemDetails(
                status: 400, errors: expectedErrors, cancellationToken: ct);
        }).Throws<System.ArgumentException>();
    }

    [Test]
    public async Task MatchesValidationProblemDetails_BodyIsLiteralJsonNull_FailsWithErrorsDiagnostic(CancellationToken ct)
    {
        // Body deserializes to a null mirror: its status and its errors are both absent, and both are listed.
        using var response = ProblemResponse(HttpStatusCode.BadRequest, "null");
        var expectedErrors = new Dictionary<string, string[]>(System.StringComparer.Ordinal)
        {
            ["FieldX"] = FieldXMessages,
        };

        var ex = await Assert.That(async () =>
        {
            await Assert.That(response).MatchesValidationProblemDetails(
                status: 400, errors: expectedErrors, cancellationToken: ct);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("but 2 entries differed:\n  status: absent (expected 400)\n  errors: absent");
    }

    [Test]
    public async Task MatchesValidationProblemDetails_ContentTypeWrong_FailsWithContentTypeDiagnostic(CancellationToken ct)
    {
        // Validation-flavor of the content-type check: bad Content-Type on the response, expect
        // the same diagnostic the non-validation path produces.
        var real = new ValidationProblemDetails(new Dictionary<string, string[]>(System.StringComparer.Ordinal)
        {
            ["FieldX"] = FieldXMessages,
        })
        {
            Status = 400,
        };
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(SerializeReal(real), Encoding.UTF8, "application/json"),
        };

        var expectedErrors = new Dictionary<string, string[]>(System.StringComparer.Ordinal)
        {
            ["FieldX"] = FieldXMessages,
        };

        var ex = await Assert.That(async () =>
        {
            await Assert.That(response).MatchesValidationProblemDetails(
                status: 400, errors: expectedErrors, cancellationToken: ct);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("application/problem+json");
        response.Dispose();
    }

    [Test]
    public async Task MatchesValidationProblemDetails_BodyNotValidJson_FailsWithParseDiagnostic(CancellationToken ct)
    {
        using var response = ProblemResponse(HttpStatusCode.BadRequest, "{not-json");
        var expectedErrors = new Dictionary<string, string[]>(System.StringComparer.Ordinal)
        {
            ["FieldX"] = FieldXMessages,
        };

        var ex = await Assert.That(async () =>
        {
            await Assert.That(response).MatchesValidationProblemDetails(
                status: 400, errors: expectedErrors, cancellationToken: ct);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("but the body did not deserialize as ProblemDetails: ");
    }

    [Test]
    public async Task MatchesValidationProblemDetails_StatusMismatch_ListsTheErrorsInTheSameList(CancellationToken ct)
    {
        // The status, field and error mismatches come in one list; on 0.6.2 a field mismatch returned before the
        // errors were compared.
        var real = new ValidationProblemDetails(new Dictionary<string, string[]>(System.StringComparer.Ordinal)
        {
            ["FieldX"] = FieldXMessages,
        })
        {
            Status = 500,
        };
        using var response = ProblemResponse(HttpStatusCode.InternalServerError, SerializeReal(real));

        var expectedErrors = new Dictionary<string, string[]>(System.StringComparer.Ordinal)
        {
            ["FieldX"] = FieldYMessages,
        };

        var ex = await Assert.That(async () =>
        {
            await Assert.That(response).MatchesValidationProblemDetails(
                status: 400, errors: expectedErrors, cancellationToken: ct);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains(
            "but 3 entries differed:\n"
            + "  HTTP status: 500 InternalServerError (expected 400)\n"
            + "  status: 500 (expected 400)\n"
            + "  errors.FieldX: [\"is required\"] (expected [\"must be positive\"])");
    }

    [Test]
    public async Task MatchesValidationProblemDetails_ErrorMessageCountDiffers_FailsWithFieldDiagnostic(CancellationToken ct)
    {
        // Same field name but different number of error messages should fail; the assertion
        // walks the actual messages vs expected and detects the length mismatch.
        var actualOne = new[] { "first" };
        var expectedTwo = new[] { "first", "second" };
        var real = new ValidationProblemDetails(new Dictionary<string, string[]>(System.StringComparer.Ordinal)
        {
            ["FieldX"] = actualOne,
        })
        {
            Status = 400,
        };
        using var response = ProblemResponse(HttpStatusCode.BadRequest, SerializeReal(real));

        var expectedErrors = new Dictionary<string, string[]>(System.StringComparer.Ordinal)
        {
            ["FieldX"] = expectedTwo,
        };

        var ex = await Assert.That(async () =>
        {
            await Assert.That(response).MatchesValidationProblemDetails(
                status: 400, errors: expectedErrors, cancellationToken: ct);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("errors.FieldX: [\"first\"] (expected [\"first\", \"second\"])");
    }

    [Test]
    public async Task MatchesValidationProblemDetails_MessageMismatch_FailsWithBothMessages(CancellationToken ct)
    {
        var actualMessages = new[] { "actual error" };
        var expectedMessages = new[] { "expected error" };
        var real = new ValidationProblemDetails(new Dictionary<string, string[]>(System.StringComparer.Ordinal)
        {
            ["FieldX"] = actualMessages,
        })
        {
            Status = 400,
        };
        using var response = ProblemResponse(HttpStatusCode.BadRequest, SerializeReal(real));

        var expectedErrors = new Dictionary<string, string[]>(System.StringComparer.Ordinal)
        {
            ["FieldX"] = expectedMessages,
        };

        var ex = await Assert.That(async () =>
        {
            await Assert.That(response).MatchesValidationProblemDetails(
                status: 400, errors: expectedErrors, cancellationToken: ct);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("FieldX");
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
            // FieldY is in the expected dictionary but absent in the actual response;
            // the assertion must surface that as a failure.
            ["FieldY"] = FieldYMessages,
        };

        var ex = await Assert.That(async () =>
        {
            await Assert.That(response).MatchesValidationProblemDetails(
                status: 400, errors: expectedErrors, cancellationToken: ct);
        }).Throws<AssertionException>();

        // Only the key that differs is listed: FieldX matches.
        await Assert.That(ex!.Message).Contains("but 1 entry differed:\n  errors.FieldY: absent (expected [\"must be positive\"])");
    }
}
