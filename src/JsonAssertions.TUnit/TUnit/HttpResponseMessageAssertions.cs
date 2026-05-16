using System;
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
