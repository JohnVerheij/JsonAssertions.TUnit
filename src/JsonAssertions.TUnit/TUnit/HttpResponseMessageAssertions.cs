using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using JsonAssertions;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace JsonAssertions.TUnit;

/// <summary>
/// Fluent TUnit entry points that apply the JSON assertions directly to an
/// <see cref="HttpResponseMessage"/>: the response body is read as text and the assertion runs
/// against it, so a test can write <c>await Assert.That(response).HasJsonProperty("user.name", ct)</c>
/// without a separate read-and-parse step. This is the assertion family's first-class HTTP
/// entry point; reading an HTTP response body is the dominant way JSON reaches a test.
/// </summary>
/// <remarks>
/// <para>Each method is generated into a TUnit assertion chain via the
/// <c>[GenerateAssertion]</c> source generator. The methods are asynchronous
/// (<see cref="Task{TResult}"/> of <see cref="AssertionResult"/>) because reading the response
/// body is asynchronous; the body read flows the supplied
/// <see cref="CancellationToken"/>, which defaults to <see cref="CancellationToken.None"/>.</para>
/// <para>A response body that is not valid JSON fails the assertion with an explained message
/// (via <see cref="JsonStringSource"/>) rather than throwing a raw <see cref="JsonException"/>.
/// The body covers only the JSON payload; status-code assertions are intentionally out of
/// scope (TUnit and HTTP-test libraries already cover those).</para>
/// </remarks>
[SuppressMessage(
    "Usage",
    "VSTHRD200:Use \"Async\" suffix for async methods",
    Justification = "These are [GenerateAssertion] source methods: the method name becomes the fluent chain entry point (Assert.That(response).HasJsonProperty(...)), so an Async suffix would corrupt the assertion surface.")]
[SuppressMessage(
    "Performance",
    "MA0109:Consider adding an overload with a Span<T> or Memory<T>",
    Justification = "The one-of overloads take T[] so callers can use a C# 12 collection expression literal (HasJsonValueOneOf(\"status\", [\"Healthy\", \"Degraded\"])); a ReadOnlySpan<T> overload cannot be expressed under TUnit's [GenerateAssertion] source generator (ref struct parameters are unsupported), and assertion call sites do not need the allocation profile a Span overload would provide.")]
public static class HttpResponseMessageAssertions
{
    /// <summary>Asserts the response body has a JSON property at the dot-separated
    /// <paramref name="path"/>.</summary>
    /// <param name="response">The HTTP response whose body is the JSON document.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.address.city</c>.</param>
    /// <param name="cancellationToken">Flows to the response-body read.</param>
    [GenerateAssertion]
    public static Task<AssertionResult> HasJsonProperty(
        this HttpResponseMessage response, string path, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);
        return AssertOnBodyAsync(response, root => JsonPropertyAssertions.HasJsonProperty(root, path), cancellationToken);
    }

    /// <summary>Asserts the response body has no JSON property at the dot-separated
    /// <paramref name="path"/>.</summary>
    /// <param name="response">The HTTP response whose body is the JSON document.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.address.city</c>.</param>
    /// <param name="cancellationToken">Flows to the response-body read.</param>
    [GenerateAssertion]
    public static Task<AssertionResult> DoesNotHaveJsonProperty(
        this HttpResponseMessage response, string path, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);
        return AssertOnBodyAsync(response, root => JsonPropertyAssertions.DoesNotHaveJsonProperty(root, path), cancellationToken);
    }

    /// <summary>Asserts the response body has the string value <paramref name="expected"/> at
    /// the dot-separated <paramref name="path"/>.</summary>
    /// <param name="response">The HTTP response whose body is the JSON document.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.name</c>.</param>
    /// <param name="expected">The expected string value.</param>
    /// <param name="cancellationToken">Flows to the response-body read.</param>
    [GenerateAssertion]
    public static Task<AssertionResult> HasJsonValue(
        this HttpResponseMessage response, string path, string expected, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);
        return AssertOnBodyAsync(response, root => JsonValueAssertions.HasJsonValue(root, path, expected), cancellationToken);
    }

    /// <summary>Asserts the response body has the boolean value <paramref name="expected"/> at
    /// the dot-separated <paramref name="path"/>.</summary>
    /// <param name="response">The HTTP response whose body is the JSON document.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.active</c>.</param>
    /// <param name="expected">The expected boolean value.</param>
    /// <param name="cancellationToken">Flows to the response-body read.</param>
    [GenerateAssertion]
    public static Task<AssertionResult> HasJsonValue(
        this HttpResponseMessage response, string path, bool expected, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);
        return AssertOnBodyAsync(response, root => JsonValueAssertions.HasJsonValue(root, path, expected), cancellationToken);
    }

    /// <summary>Asserts the response body has the numeric value <paramref name="expected"/> at
    /// the dot-separated <paramref name="path"/>.</summary>
    /// <param name="response">The HTTP response whose body is the JSON document.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.age</c>.</param>
    /// <param name="expected">The expected numeric value.</param>
    /// <param name="cancellationToken">Flows to the response-body read.</param>
    [GenerateAssertion]
    public static Task<AssertionResult> HasJsonValue(
        this HttpResponseMessage response, string path, double expected, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);
        return AssertOnBodyAsync(response, root => JsonValueAssertions.HasJsonValue(root, path, expected), cancellationToken);
    }

    /// <summary>Asserts the response body has the 32-bit integer value <paramref name="expected"/>
    /// at the dot-separated <paramref name="path"/>, matching whether the JSON encodes it as a number
    /// or as a numeric string and comparing exactly. System.Text.Json writes <c>int32</c> as a JSON
    /// number while Protobuf's <c>JsonFormatter</c> can emit a JSON string; both are matched.</summary>
    /// <param name="response">The HTTP response whose body is the JSON document.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.age</c>.</param>
    /// <param name="expected">The expected 32-bit integer value.</param>
    /// <param name="cancellationToken">Flows to the response-body read.</param>
    [GenerateAssertion]
    public static Task<AssertionResult> HasJsonValue(
        this HttpResponseMessage response, string path, int expected, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);
        return AssertOnBodyAsync(response, root => JsonValueAssertions.HasJsonValue(root, path, expected), cancellationToken);
    }

    /// <summary>Asserts the response body has the unsigned 32-bit integer value
    /// <paramref name="expected"/> at the dot-separated <paramref name="path"/>, matching whether the
    /// JSON encodes it as a number or as a numeric string and comparing exactly. System.Text.Json
    /// writes <c>uint32</c> as a JSON number while Protobuf's <c>JsonFormatter</c> can emit a JSON
    /// string; both are matched.</summary>
    /// <param name="response">The HTTP response whose body is the JSON document.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.age</c>.</param>
    /// <param name="expected">The expected unsigned 32-bit integer value.</param>
    /// <param name="cancellationToken">Flows to the response-body read.</param>
    [GenerateAssertion]
    public static Task<AssertionResult> HasJsonValue(
        this HttpResponseMessage response, string path, uint expected, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);
        return AssertOnBodyAsync(response, root => JsonValueAssertions.HasJsonValue(root, path, expected), cancellationToken);
    }

    /// <summary>Asserts the response body has the 64-bit integer value <paramref name="expected"/>
    /// at the dot-separated <paramref name="path"/>, matching whether the JSON encodes it as a number
    /// or as a numeric string (parsed with <see cref="System.Globalization.CultureInfo.InvariantCulture"/>) and
    /// comparing exactly. Protobuf serializes <c>int64</c> as a JSON string while System.Text.Json
    /// writes it as a JSON number; both are matched.</summary>
    /// <param name="response">The HTTP response whose body is the JSON document.</param>
    /// <param name="path">A dot-separated property path, for example <c>guid.high</c>.</param>
    /// <param name="expected">The expected 64-bit integer value.</param>
    /// <param name="cancellationToken">Flows to the response-body read.</param>
    [GenerateAssertion]
    public static Task<AssertionResult> HasJsonValue(
        this HttpResponseMessage response, string path, long expected, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);
        return AssertOnBodyAsync(response, root => JsonValueAssertions.HasJsonValue(root, path, expected), cancellationToken);
    }

    /// <summary>Asserts the response body has the unsigned 64-bit integer value
    /// <paramref name="expected"/> at the dot-separated <paramref name="path"/>, matching whether the
    /// JSON encodes it as a number or as a numeric string (parsed with
    /// <see cref="System.Globalization.CultureInfo.InvariantCulture"/>) and comparing exactly. Protobuf serializes
    /// <c>uint64</c> as a JSON string while System.Text.Json writes it as a JSON number; both are
    /// matched.</summary>
    /// <param name="response">The HTTP response whose body is the JSON document.</param>
    /// <param name="path">A dot-separated property path, for example <c>guid.low</c>.</param>
    /// <param name="expected">The expected unsigned 64-bit integer value.</param>
    /// <param name="cancellationToken">Flows to the response-body read.</param>
    [GenerateAssertion]
    public static Task<AssertionResult> HasJsonValue(
        this HttpResponseMessage response, string path, ulong expected, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);
        return AssertOnBodyAsync(response, root => JsonValueAssertions.HasJsonValue(root, path, expected), cancellationToken);
    }

    /// <summary>Asserts the value at the dot-separated <paramref name="path"/> in the response
    /// body is a JSON array with exactly <paramref name="expectedLength"/> elements.</summary>
    /// <param name="response">The HTTP response whose body is the JSON document.</param>
    /// <param name="path">A dot-separated property path, for example <c>items</c>.</param>
    /// <param name="expectedLength">The expected array length.</param>
    /// <param name="cancellationToken">Flows to the response-body read.</param>
    [GenerateAssertion]
    public static Task<AssertionResult> HasJsonArrayLength(
        this HttpResponseMessage response, string path, int expectedLength, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);
        return AssertOnBodyAsync(response, root => JsonShapeAssertions.HasJsonArrayLength(root, path, expectedLength), cancellationToken);
    }

    /// <summary>Asserts the value at the dot-separated <paramref name="path"/> in the response
    /// body is a JSON array with at least one element.</summary>
    /// <param name="response">The HTTP response whose body is the JSON document.</param>
    /// <param name="path">A dot-separated property path, for example <c>items</c>.</param>
    /// <param name="cancellationToken">Flows to the response-body read.</param>
    [GenerateAssertion]
    public static Task<AssertionResult> HasNonEmptyJsonArray(
        this HttpResponseMessage response, string path, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);
        return AssertOnBodyAsync(response, root => JsonShapeAssertions.HasNonEmptyJsonArray(root, path), cancellationToken);
    }

    /// <summary>Asserts the value at the dot-separated <paramref name="path"/> in the response
    /// body is a JSON array with no elements.</summary>
    /// <param name="response">The HTTP response whose body is the JSON document.</param>
    /// <param name="path">A dot-separated property path, for example <c>items</c>.</param>
    /// <param name="cancellationToken">Flows to the response-body read.</param>
    [GenerateAssertion]
    public static Task<AssertionResult> HasEmptyJsonArray(
        this HttpResponseMessage response, string path, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);
        return AssertOnBodyAsync(response, root => JsonShapeAssertions.HasEmptyJsonArray(root, path), cancellationToken);
    }

    /// <summary>Asserts the value at the dot-separated <paramref name="path"/> in the response
    /// body is of the given <see cref="JsonValueKind"/>.</summary>
    /// <param name="response">The HTTP response whose body is the JSON document.</param>
    /// <param name="path">A dot-separated property path, for example <c>data</c>.</param>
    /// <param name="expectedKind">The expected JSON value kind.</param>
    /// <param name="cancellationToken">Flows to the response-body read.</param>
    [GenerateAssertion]
    public static Task<AssertionResult> HasJsonValueKind(
        this HttpResponseMessage response, string path, JsonValueKind expectedKind, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);
        return AssertOnBodyAsync(response, root => JsonShapeAssertions.HasJsonValueKind(root, path, expectedKind), cancellationToken);
    }

    /// <summary>Asserts the value at <paramref name="path"/> in the response body is a
    /// non-empty JSON string.</summary>
    /// <param name="response">The HTTP response whose body is the JSON document.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>user.name</c>.</param>
    /// <param name="cancellationToken">Flows to the response-body read.</param>
    [GenerateAssertion]
    public static Task<AssertionResult> HasNonEmptyJsonString(
        this HttpResponseMessage response, string path, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);
        return AssertOnBodyAsync(response, root => JsonShapeAssertions.HasNonEmptyJsonString(root, path), cancellationToken);
    }

    /// <summary>Asserts the value at <paramref name="path"/> in the response body is a JSON
    /// boolean (either <see langword="true"/> or <see langword="false"/>).</summary>
    /// <param name="response">The HTTP response whose body is the JSON document.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>user.active</c>.</param>
    /// <param name="cancellationToken">Flows to the response-body read.</param>
    [GenerateAssertion]
    public static Task<AssertionResult> HasJsonBoolean(
        this HttpResponseMessage response, string path, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);
        return AssertOnBodyAsync(response, root => JsonShapeAssertions.HasJsonBoolean(root, path), cancellationToken);
    }

    /// <summary>Asserts the value at <paramref name="path"/> in the response body satisfies
    /// <paramref name="predicate"/>.</summary>
    /// <param name="response">The HTTP response whose body is the JSON document.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>user.age</c>.</param>
    /// <param name="predicate">A predicate that returns <see langword="true"/> for matching
    /// elements.</param>
    /// <param name="cancellationToken">Flows to the response-body read.</param>
    [GenerateAssertion]
    public static Task<AssertionResult> HasJsonValueMatching(
        this HttpResponseMessage response, string path, Func<JsonElement, bool> predicate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(predicate);
        return AssertOnBodyAsync(response, root => JsonValueAssertions.HasJsonValueMatching(root, path, predicate), cancellationToken);
    }

    /// <summary>Asserts the value at <paramref name="path"/> in the response body is a JSON
    /// string equal (ordinal) to any of <paramref name="candidates"/>.</summary>
    /// <param name="response">The HTTP response whose body is the JSON document.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>health.status</c>.</param>
    /// <param name="candidates">The acceptable string values.</param>
    /// <param name="cancellationToken">Flows to the response-body read.</param>
    [GenerateAssertion]
    public static Task<AssertionResult> HasJsonValueOneOf(
        this HttpResponseMessage response, string path, string[] candidates, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(candidates);
        return AssertOnBodyAsync(response, root => JsonValueAssertions.HasJsonValueOneOf(root, path, candidates), cancellationToken);
    }

    /// <summary>Asserts the value at <paramref name="path"/> in the response body is a JSON
    /// number equal to any of <paramref name="candidates"/>.</summary>
    /// <param name="response">The HTTP response whose body is the JSON document.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>response.code</c>.</param>
    /// <param name="candidates">The acceptable numeric values.</param>
    /// <param name="cancellationToken">Flows to the response-body read.</param>
    [GenerateAssertion]
    public static Task<AssertionResult> HasJsonValueOneOf(
        this HttpResponseMessage response, string path, double[] candidates, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(candidates);
        return AssertOnBodyAsync(response, root => JsonValueAssertions.HasJsonValueOneOf(root, path, candidates), cancellationToken);
    }

    /// <summary>Asserts the value at <paramref name="path"/> in the response body is a 32-bit integer
    /// equal to any of <paramref name="candidates"/>, encoded as either a JSON number or a numeric
    /// JSON string, for System.Text.Json or Protobuf-style <c>int32</c> values.</summary>
    /// <param name="response">The HTTP response whose body is the JSON document.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>response.code</c>.</param>
    /// <param name="candidates">The acceptable 32-bit integer values.</param>
    /// <param name="cancellationToken">Flows to the response-body read.</param>
    [GenerateAssertion]
    public static Task<AssertionResult> HasJsonValueOneOf(
        this HttpResponseMessage response, string path, int[] candidates, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(candidates);
        return AssertOnBodyAsync(response, root => JsonValueAssertions.HasJsonValueOneOf(root, path, candidates), cancellationToken);
    }

    /// <summary>Asserts the value at <paramref name="path"/> in the response body is an unsigned
    /// 32-bit integer equal to any of <paramref name="candidates"/>, encoded as either a JSON number
    /// or a numeric JSON string, for System.Text.Json or Protobuf-style <c>uint32</c>
    /// values.</summary>
    /// <param name="response">The HTTP response whose body is the JSON document.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>response.code</c>.</param>
    /// <param name="candidates">The acceptable unsigned 32-bit integer values.</param>
    /// <param name="cancellationToken">Flows to the response-body read.</param>
    [GenerateAssertion]
    public static Task<AssertionResult> HasJsonValueOneOf(
        this HttpResponseMessage response, string path, uint[] candidates, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(candidates);
        return AssertOnBodyAsync(response, root => JsonValueAssertions.HasJsonValueOneOf(root, path, candidates), cancellationToken);
    }

    /// <summary>Asserts the value at <paramref name="path"/> in the response body is a 64-bit integer
    /// equal to any of <paramref name="candidates"/>, encoded as either a JSON number or a numeric
    /// JSON string (parsed with <see cref="System.Globalization.CultureInfo.InvariantCulture"/>), for
    /// System.Text.Json or Protobuf-style <c>int64</c> values.</summary>
    /// <param name="response">The HTTP response whose body is the JSON document.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>message.sequence</c>.</param>
    /// <param name="candidates">The acceptable 64-bit integer values.</param>
    /// <param name="cancellationToken">Flows to the response-body read.</param>
    [GenerateAssertion]
    public static Task<AssertionResult> HasJsonValueOneOf(
        this HttpResponseMessage response, string path, long[] candidates, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(candidates);
        return AssertOnBodyAsync(response, root => JsonValueAssertions.HasJsonValueOneOf(root, path, candidates), cancellationToken);
    }

    /// <summary>Asserts the value at <paramref name="path"/> in the response body is an unsigned
    /// 64-bit integer equal to any of <paramref name="candidates"/>, encoded as either a JSON number
    /// or a numeric JSON string (parsed with <see cref="System.Globalization.CultureInfo.InvariantCulture"/>), for
    /// System.Text.Json or Protobuf-style <c>uint64</c> values.</summary>
    /// <param name="response">The HTTP response whose body is the JSON document.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>message.sequence</c>.</param>
    /// <param name="candidates">The acceptable unsigned 64-bit integer values.</param>
    /// <param name="cancellationToken">Flows to the response-body read.</param>
    [GenerateAssertion]
    public static Task<AssertionResult> HasJsonValueOneOf(
        this HttpResponseMessage response, string path, ulong[] candidates, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(candidates);
        return AssertOnBodyAsync(response, root => JsonValueAssertions.HasJsonValueOneOf(root, path, candidates), cancellationToken);
    }

    /// <summary>Asserts the value at <paramref name="path"/> in the response body is a JSON
    /// string whose text parses as <typeparamref name="T"/> via
    /// <see cref="IParsable{T}.TryParse(string, IFormatProvider, out T)"/>.</summary>
    /// <typeparam name="T">The target type implementing <see cref="IParsable{T}"/>.</typeparam>
    /// <param name="response">The HTTP response whose body is the JSON document.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>order.id</c>.</param>
    /// <param name="cancellationToken">Flows to the response-body read.</param>
    [GenerateAssertion]
    public static Task<AssertionResult> HasJsonValueParsableAs<T>(
        this HttpResponseMessage response, string path, CancellationToken cancellationToken = default)
        where T : IParsable<T>
    {
        ArgumentNullException.ThrowIfNull(response);
        return AssertOnBodyAsync(response, root => JsonValueAssertions.HasJsonValueParsableAs<T>(root, path), cancellationToken);
    }

    /// <summary>Asserts the response has the expected HTTP status code AND the body
    /// deserializes (via the supplied <paramref name="jsonTypeInfo"/>) to a value structurally
    /// equal to <paramref name="expected"/> under <see cref="object.Equals(object, object)"/>.
    /// AOT-clean: the supplied <see cref="JsonTypeInfo{T}"/> is the source-generated entry for
    /// <typeparamref name="T"/> in the consumer's <c>JsonSerializerContext</c>; the assertion
    /// uses no runtime reflection. Failure messages include the response body (truncated at
    /// 256 chars) so the diagnostic surfaces the structured-error shape for non-200 responses
    /// and the actual JSON shape for deserialization failures.</summary>
    /// <typeparam name="T">The expected response-body type.</typeparam>
    /// <param name="response">The HTTP response.</param>
    /// <param name="expectedStatus">The expected HTTP status code.</param>
    /// <param name="jsonTypeInfo">The <see cref="JsonTypeInfo{T}"/> for AOT-clean deserialization.</param>
    /// <param name="expected">The expected deserialized value (compared by <see cref="object.Equals(object, object)"/>).</param>
    /// <param name="cancellationToken">Flows to the response-body read.</param>
    /// <returns>A passed assertion when status and value both match; otherwise a failed assertion
    /// with a body-aware diagnostic message.</returns>
    /// <exception cref="ArgumentNullException">A required argument is <see langword="null"/>.</exception>
    [GenerateAssertion]
    public static async Task<AssertionResult> HasJsonResponse<T>(
        this HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        JsonTypeInfo<T> jsonTypeInfo,
        T expected,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(jsonTypeInfo);

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (response.StatusCode != expectedStatus)
        {
            return AssertionResult.Failed(
                JsonFailureMessage.ResponseStatusMismatch(expectedStatus, response.StatusCode, body));
        }

        T? actual;
        try
        {
            actual = JsonSerializer.Deserialize(body, jsonTypeInfo);
        }
        catch (JsonException ex)
        {
            return AssertionResult.Failed(
                JsonFailureMessage.ResponseDeserializationFailed(jsonTypeInfo.Type.Name, body, ex));
        }

        return Equals(actual, expected)
            ? AssertionResult.Passed
            : AssertionResult.Failed(
                JsonFailureMessage.ResponseValueMismatch(jsonTypeInfo.Type.Name, actual, expected));
    }

    /// <summary>Asserts the response is a valid RFC 7807 ProblemDetails (Content-Type
    /// <c>application/problem+json</c>, deserializable shape) and that each specified field
    /// matches. Unspecified fields are not asserted; pass <see langword="null"/> to skip a field.
    /// Internal mirror type avoids the <c>Microsoft.AspNetCore.Mvc.Abstractions</c> dependency
    /// (Apache 2.0) so the package stays MIT-clean and AOT-clean.</summary>
    /// <param name="response">The HTTP response.</param>
    /// <param name="status">The expected <c>status</c> field value.</param>
    /// <param name="title">The expected <c>title</c> field value, or <see langword="null"/> to skip.</param>
    /// <param name="detail">The expected <c>detail</c> field value, or <see langword="null"/> to skip.</param>
    /// <param name="type">The expected <c>type</c> URI field value, or <see langword="null"/> to skip.</param>
    /// <param name="instance">The expected <c>instance</c> URI field value, or <see langword="null"/> to skip.</param>
    /// <param name="cancellationToken">Flows to the response-body read.</param>
    [GenerateAssertion]
    public static async Task<AssertionResult> MatchesProblemDetails(
        this HttpResponseMessage response,
        int status,
        string? title = null,
        string? detail = null,
        string? type = null,
        string? instance = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);

        var contentTypeCheck = await ReadAndValidateProblemDetailsContentTypeAsync(response, cancellationToken).ConfigureAwait(false);
        if (contentTypeCheck.Failure is not null)
        {
            return AssertionResult.Failed(contentTypeCheck.Failure);
        }

        ProblemDetailsMirror? mirror;
        try
        {
            mirror = JsonSerializer.Deserialize(contentTypeCheck.Body, ProblemDetailsMirrorJsonContext.Default.ProblemDetailsMirror);
        }
        catch (JsonException ex)
        {
            return AssertionResult.Failed(JsonFailureMessage.ProblemDetailsParseFailed(contentTypeCheck.Body, ex));
        }

        return CompareProblemDetailsFields(mirror, status, title, detail, type, instance);
    }

    /// <summary>Asserts the response is a valid RFC 7807 ValidationProblemDetails (the
    /// ProblemDetails extension with an <c>errors</c> dictionary mapping field names to
    /// validation messages, the ASP.NET Core convention). Both the base ProblemDetails fields
    /// and the <paramref name="errors"/> dictionary are asserted.</summary>
    /// <param name="response">The HTTP response.</param>
    /// <param name="status">The expected <c>status</c> field value.</param>
    /// <param name="errors">The expected validation errors keyed by field name. Every key in
    /// this dictionary must appear in the response's errors dict with matching values.</param>
    /// <param name="title">The expected <c>title</c> field value, or <see langword="null"/> to skip.</param>
    /// <param name="detail">The expected <c>detail</c> field value, or <see langword="null"/> to skip.</param>
    /// <param name="type">The expected <c>type</c> URI field value, or <see langword="null"/> to skip.</param>
    /// <param name="instance">The expected <c>instance</c> URI field value, or <see langword="null"/> to skip.</param>
    /// <param name="cancellationToken">Flows to the response-body read.</param>
    [GenerateAssertion]
    public static async Task<AssertionResult> MatchesValidationProblemDetails(
        this HttpResponseMessage response,
        int status,
        IReadOnlyDictionary<string, string[]> errors,
        string? title = null,
        string? detail = null,
        string? type = null,
        string? instance = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(errors);

        var contentTypeCheck = await ReadAndValidateProblemDetailsContentTypeAsync(response, cancellationToken).ConfigureAwait(false);
        if (contentTypeCheck.Failure is not null)
        {
            return AssertionResult.Failed(contentTypeCheck.Failure);
        }

        ValidationProblemDetailsMirror? mirror;
        try
        {
            mirror = JsonSerializer.Deserialize(contentTypeCheck.Body, ProblemDetailsMirrorJsonContext.Default.ValidationProblemDetailsMirror);
        }
        catch (JsonException ex)
        {
            return AssertionResult.Failed(JsonFailureMessage.ProblemDetailsParseFailed(contentTypeCheck.Body, ex));
        }

        var fieldsResult = CompareProblemDetailsFields(mirror, status, title, detail, type, instance);
        if (!fieldsResult.IsPassed)
        {
            return fieldsResult;
        }

        return CompareValidationErrors(mirror?.Errors, errors);
    }

    /// <summary>Reads the response body and verifies the Content-Type matches RFC 7807's
    /// <c>application/problem+json</c>. Returns the body for subsequent deserialization and a
    /// pre-rendered failure message if the Content-Type is wrong.</summary>
    private static async Task<(string Body, string? Failure)> ReadAndValidateProblemDetailsContentTypeAsync(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var contentType = response.Content.Headers.ContentType?.MediaType;
        // Media-type tokens are case-insensitive per RFC 9110 §8.3.2; a valid Application/Problem+Json must match.
        return string.Equals(contentType, "application/problem+json", StringComparison.OrdinalIgnoreCase)
            ? (body, null)
            : (body, JsonFailureMessage.ProblemDetailsContentTypeMismatch(contentType, body));
    }

    /// <summary>Compares the specified ProblemDetails fields against the deserialized mirror,
    /// collecting all mismatches into a single failure message.</summary>
    private static AssertionResult CompareProblemDetailsFields(
        ProblemDetailsMirror? mirror, int status, string? title, string? detail, string? type, string? instance)
    {
        var mismatches = new List<(string Field, string? Expected, string? Actual)>();

        if (mirror?.Status != status)
        {
            mismatches.Add(("status", status.ToString(System.Globalization.CultureInfo.InvariantCulture),
                mirror?.Status?.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }
        if (title is not null && !string.Equals(mirror?.Title, title, StringComparison.Ordinal))
        {
            mismatches.Add(("title", title, mirror?.Title));
        }
        if (detail is not null && !string.Equals(mirror?.Detail, detail, StringComparison.Ordinal))
        {
            mismatches.Add(("detail", detail, mirror?.Detail));
        }
        if (type is not null && !string.Equals(mirror?.Type, type, StringComparison.Ordinal))
        {
            mismatches.Add(("type", type, mirror?.Type));
        }
        if (instance is not null && !string.Equals(mirror?.Instance, instance, StringComparison.Ordinal))
        {
            mismatches.Add(("instance", instance, mirror?.Instance));
        }

        return mismatches.Count is 0
            ? AssertionResult.Passed
            : AssertionResult.Failed(JsonFailureMessage.ProblemDetailsFieldMismatch(mismatches));
    }

    /// <summary>Compares the actual ValidationProblemDetails <c>errors</c> dictionary against
    /// the expected one. Every key in <paramref name="expected"/> must appear in
    /// <paramref name="actual"/> with matching value arrays.</summary>
    private static AssertionResult CompareValidationErrors(
        Dictionary<string, string[]>? actual,
        IReadOnlyDictionary<string, string[]> expected)
    {
        if (actual is null && expected.Count > 0)
        {
            return AssertionResult.Failed(JsonFailureMessage.ValidationErrorsMismatch(expected, actual: null));
        }

        var hasMismatch = false;
        foreach (var kvp in expected)
        {
            if (!actual!.TryGetValue(kvp.Key, out var actualMessages))
            {
                hasMismatch = true;
                break;
            }
            if (actualMessages.Length != kvp.Value.Length)
            {
                hasMismatch = true;
                break;
            }
            for (var i = 0; i < kvp.Value.Length; i++)
            {
                if (!string.Equals(actualMessages[i], kvp.Value[i], StringComparison.Ordinal))
                {
                    hasMismatch = true;
                    break;
                }
            }
            if (hasMismatch)
                break;
        }

        return hasMismatch
            ? AssertionResult.Failed(JsonFailureMessage.ValidationErrorsMismatch(expected, actual))
            : AssertionResult.Passed;
    }

    /// <summary>Asserts the response body contains <paramref name="expectedSubset"/> as a JSON
    /// subset: every property in the expected document must be present in the body with an
    /// equivalent value (recursively), while the body may carry additional properties. The failure
    /// lists every missing or mismatched field.</summary>
    /// <param name="response">The HTTP response whose body is the JSON document.</param>
    /// <param name="expectedSubset">The expected subset document text.</param>
    /// <param name="cancellationToken">Flows to the response-body read.</param>
    [GenerateAssertion]
    public static Task<AssertionResult> ContainsJson(
        this HttpResponseMessage response, string expectedSubset, CancellationToken cancellationToken = default)
        => ContainsJson(response, expectedSubset, configure: null, cancellationToken);

    /// <summary>Asserts the response body contains <paramref name="expectedSubset"/> as a JSON
    /// subset, with comparison options (ignored paths, order-insensitive arrays) set by
    /// <paramref name="configure"/>.</summary>
    /// <param name="response">The HTTP response whose body is the JSON document.</param>
    /// <param name="expectedSubset">The expected subset document text.</param>
    /// <param name="configure">A callback that sets comparison options. May be
    /// <see langword="null"/> for the defaults.</param>
    /// <param name="cancellationToken">Flows to the response-body read.</param>
    [GenerateAssertion]
    public static Task<AssertionResult> ContainsJson(
        this HttpResponseMessage response, string expectedSubset, Action<JsonEquivalenceOptions>? configure, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(expectedSubset);
        var options = JsonContainsAssertions.BuildOptions(configure);
        return AssertOnBodyAsync(response, root => JsonContainsAssertions.ContainsAgainst(root, expectedSubset, options), cancellationToken);
    }

    /// <summary>Reads the response body as text and runs <paramref name="assert"/> against the
    /// parsed root element, mapping a parse failure to an explained assertion failure.</summary>
    private static async Task<AssertionResult> AssertOnBodyAsync(
        HttpResponseMessage response,
        Func<JsonElement, AssertionResult> assert,
        CancellationToken cancellationToken)
    {
        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return JsonStringSource.Assert(json, assert);
    }
}
