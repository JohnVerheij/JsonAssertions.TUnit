using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Text.Json;
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
