using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace JsonAssertions;

/// <summary>
/// Extraction helpers that read a typed value out of a JSON document at a dot/bracket path and
/// return it, for tests that need the value rather than an assertion: pulling an id from a response
/// to drive the next request, or reading a count to cross-check against an array length. Each helper
/// navigates with the same <see cref="JsonPath"/> resolver the assertions use and throws a
/// <see cref="JsonExtractionException"/> whose message says where the path stopped and why, replacing
/// a hand-rolled <c>JsonDocument.Parse(...).RootElement.GetProperty(...).GetInt32()</c> chain (which
/// throws bare BCL exceptions with no path context). These are plain extension methods, not TUnit
/// assertions; the value they return is used directly. A malformed path throws
/// <see cref="ArgumentException"/> before any navigation, as does the <c>[*]</c> wildcard in a method
/// that reads one value (<c>GetJsonValues</c> reads every value it expands to); a path that does
/// not resolve throws <see cref="JsonExtractionException"/>.
/// </summary>
public static class JsonExtraction
{
    /// <summary>Reads the value at <paramref name="path"/> and parses it as <typeparamref name="T"/>.
    /// A JSON string is parsed from its text; a JSON number or boolean is parsed from its literal
    /// (so <c>GetJsonValue&lt;int&gt;("cycleId")</c> reads either <c>42</c> or <c>"42"</c>). Parsing
    /// uses <see cref="CultureInfo.InvariantCulture"/>.</summary>
    /// <typeparam name="T">The target type implementing <see cref="IParsable{T}"/> (for example
    /// <see cref="int"/>, <see cref="long"/>, <see cref="bool"/>, <see cref="Guid"/>,
    /// <see cref="DateTimeOffset"/>).</typeparam>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket indices.</param>
    /// <returns>The parsed value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is malformed, or has a <c>[*]</c> wildcard (use
    /// <see cref="GetJsonValues{T}(JsonElement, string)"/> for every value).</exception>
    /// <exception cref="JsonExtractionException">The path does not resolve, or the value does not
    /// parse as <typeparamref name="T"/>.</exception>
    public static T GetJsonValue<T>(this JsonElement element, string path)
        where T : IParsable<T>
    {
        ArgumentNullException.ThrowIfNull(path);
        var resolution = Resolve(element, path);

        if (TryParseValue<T>(resolution.Element, out var value))
        {
            return value;
        }

        throw new JsonExtractionException(
            JsonFailureMessage.ExtractionUnreadable(path, resolution.Element, "which does not parse as " + typeof(T).Name));
    }

    /// <summary>Parses <paramref name="json"/> and reads the value at <paramref name="path"/> as
    /// <typeparamref name="T"/>. See <see cref="GetJsonValue{T}(JsonElement, string)"/>.</summary>
    /// <typeparam name="T">The target type implementing <see cref="IParsable{T}"/>.</typeparam>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket indices.</param>
    /// <returns>The parsed value.</returns>
    /// <exception cref="ArgumentNullException">A required argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is malformed or has a <c>[*]</c> wildcard;
    /// checked after the text is parsed.</exception>
    /// <exception cref="JsonExtractionException">The text is not valid JSON, the path does not resolve,
    /// or the value does not parse as <typeparamref name="T"/>.</exception>
    public static T GetJsonValue<T>(this string json, string path)
        where T : IParsable<T>
    {
        using var document = ParseOrThrow(json);
        return document.RootElement.GetJsonValue<T>(path);
    }

    /// <summary>Reads every value that the <c>[*]</c> wildcard in <paramref name="path"/> expands to, in document
    /// order, each parsed as <see cref="GetJsonValue{T}(JsonElement, string)"/> parses one value. An empty array gives
    /// an empty list. Every element must resolve and parse; otherwise the exception lists each one that does not, by
    /// its concrete path, so no value is dropped silently.</summary>
    /// <typeparam name="T">The target type implementing <see cref="IParsable{T}"/>.</typeparam>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A path with at least one <c>[*]</c> wildcard, for example <c>entries[*].name</c>.</param>
    /// <returns>The parsed values.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is malformed, or has no <c>[*]</c> wildcard (use
    /// <see cref="GetJsonValue{T}(JsonElement, string)"/> for one value).</exception>
    /// <exception cref="JsonExtractionException">An element does not resolve or does not parse as
    /// <typeparamref name="T"/>, or <paramref name="element"/> is <see langword="default"/>.</exception>
    public static IReadOnlyList<T> GetJsonValues<T>(this JsonElement element, string path)
        where T : IParsable<T>
    {
        RequireWildcardPath(path);
        if (element.ValueKind is JsonValueKind.Undefined)
        {
            throw new JsonExtractionException(JsonFailureMessage.Sentence(JsonFailureMessage.DefaultElement));
        }

        var resolutions = JsonPath.ResolveAll(element, path);
        var values = new List<T>(resolutions.Count);
        var failures = new List<string>();
        foreach (var resolution in resolutions)
        {
            if (!resolution.Found)
            {
                failures.Add("'" + JsonFailureMessage.FailedLocation(resolution) + "': " + JsonFailureMessage.DescribeFailure(resolution));
            }
            else if (TryParseValue<T>(resolution.Element, out var value))
            {
                values.Add(value);
            }
            else
            {
                failures.Add("'" + resolution.ResolvedPrefix + "': " + JsonFailureMessage.ValueNotParsed(resolution.Element, typeof(T).Name));
            }
        }

        return failures.Count is 0
            ? values
            : throw new JsonExtractionException(JsonFailureMessage.ValuesNotRead(path, failures, resolutions.Count));
    }

    /// <summary>Parses <paramref name="json"/> and reads every value that the <c>[*]</c> wildcard in
    /// <paramref name="path"/> expands to. See <see cref="GetJsonValues{T}(JsonElement, string)"/>.</summary>
    /// <typeparam name="T">The target type implementing <see cref="IParsable{T}"/>.</typeparam>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A path with at least one <c>[*]</c> wildcard.</param>
    /// <returns>The parsed values.</returns>
    /// <exception cref="ArgumentNullException">A required argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is malformed or has no <c>[*]</c> wildcard;
    /// checked before the text is parsed.</exception>
    /// <exception cref="JsonExtractionException">The text is not valid JSON, or an element does not resolve or does
    /// not parse as <typeparamref name="T"/>.</exception>
    public static IReadOnlyList<T> GetJsonValues<T>(this string json, string path)
        where T : IParsable<T>
    {
        RequireWildcardPath(path);
        using var document = ParseOrThrow(json);
        return document.RootElement.GetJsonValues<T>(path);
    }

    /// <summary>Reads the response body and every value that the <c>[*]</c> wildcard in <paramref name="path"/>
    /// expands to. See <see cref="GetJsonValues{T}(JsonElement, string)"/>.</summary>
    /// <typeparam name="T">The target type implementing <see cref="IParsable{T}"/>.</typeparam>
    /// <param name="response">The HTTP response whose body is the JSON document.</param>
    /// <param name="path">A path with at least one <c>[*]</c> wildcard.</param>
    /// <param name="cancellationToken">Flows to the response-body read.</param>
    /// <returns>The parsed values.</returns>
    /// <exception cref="ArgumentNullException">A required argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is malformed or has no <c>[*]</c> wildcard;
    /// checked before the body is read.</exception>
    /// <exception cref="JsonExtractionException">The body is empty or not valid JSON, or an element does not resolve
    /// or does not parse as <typeparamref name="T"/>.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> is canceled, also when the
    /// body is already in memory.</exception>
    public static async Task<IReadOnlyList<T>> GetJsonValuesAsync<T>(
        this HttpResponseMessage response, string path, CancellationToken cancellationToken = default)
        where T : IParsable<T>
    {
        ArgumentNullException.ThrowIfNull(response);
        RequireWildcardPath(path);
        var json = await ReadBodyAsync(response, cancellationToken).ConfigureAwait(false);
        return json.GetJsonValues<T>(path);
    }

    /// <summary>Reads the JSON string value at <paramref name="path"/>. The value must be a JSON
    /// string (a number, boolean, object, or array is a failure; use <see cref="GetJsonValue{T}(JsonElement, string)"/>
    /// or <see cref="GetJsonElement(JsonElement, string)"/> for those).</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket indices.</param>
    /// <returns>The string value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is malformed or has a <c>[*]</c> wildcard.</exception>
    /// <exception cref="JsonExtractionException">The path does not resolve, or the value is not a JSON string.</exception>
    public static string GetJsonString(this JsonElement element, string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        var resolution = Resolve(element, path);

        if (resolution.Element.ValueKind is JsonValueKind.String)
        {
            return resolution.Element.GetString()!;
        }

        throw new JsonExtractionException(
            JsonFailureMessage.ExtractionUnreadable(path, resolution.Element, "which is not a JSON string"));
    }

    /// <summary>Parses <paramref name="json"/> and reads the JSON string value at
    /// <paramref name="path"/>. See <see cref="GetJsonString(JsonElement, string)"/>.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket indices.</param>
    /// <returns>The string value.</returns>
    /// <exception cref="ArgumentNullException">A required argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is malformed or has a <c>[*]</c> wildcard;
    /// checked after the text is parsed.</exception>
    /// <exception cref="JsonExtractionException">The text is not valid JSON, the path does not resolve,
    /// or the value is not a JSON string.</exception>
    public static string GetJsonString(this string json, string path)
    {
        using var document = ParseOrThrow(json);
        return document.RootElement.GetJsonString(path);
    }

    /// <summary>Reads the JSON element (any kind) at <paramref name="path"/> as a detached copy that
    /// stays valid after the source document is disposed, for grabbing an object or array subtree to
    /// inspect or enumerate.</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket indices.</param>
    /// <returns>A detached (<see cref="JsonElement.Clone"/>d) copy of the element at the path.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is malformed or has a <c>[*]</c> wildcard.</exception>
    /// <exception cref="JsonExtractionException">The path does not resolve.</exception>
    public static JsonElement GetJsonElement(this JsonElement element, string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return Resolve(element, path).Element.Clone();
    }

    /// <summary>Parses <paramref name="json"/> and reads the JSON element at <paramref name="path"/>
    /// as a detached copy. See <see cref="GetJsonElement(JsonElement, string)"/>.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket indices.</param>
    /// <returns>A detached copy of the element at the path.</returns>
    /// <exception cref="ArgumentNullException">A required argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is malformed or has a <c>[*]</c> wildcard;
    /// checked after the text is parsed.</exception>
    /// <exception cref="JsonExtractionException">The text is not valid JSON, or the path does not resolve.</exception>
    public static JsonElement GetJsonElement(this string json, string path)
    {
        using var document = ParseOrThrow(json);
        return document.RootElement.GetJsonElement(path);
    }

    /// <summary>Reads the response body and reads the value at <paramref name="path"/> as
    /// <typeparamref name="T"/>. See <see cref="GetJsonValue{T}(JsonElement, string)"/>. When the
    /// same response is queried for several values, prefer reading it once to a
    /// <see cref="JsonElement"/> and using the element overloads.</summary>
    /// <typeparam name="T">The target type implementing <see cref="IParsable{T}"/>.</typeparam>
    /// <param name="response">The HTTP response whose body is the JSON document.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket indices.</param>
    /// <param name="cancellationToken">Flows to the response-body read.</param>
    /// <returns>The parsed value.</returns>
    /// <exception cref="ArgumentNullException">A required argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is malformed or has a <c>[*]</c> wildcard;
    /// checked after the body is read.</exception>
    /// <exception cref="JsonExtractionException">The body is empty or not valid JSON, the path does not
    /// resolve, or the value does not parse as <typeparamref name="T"/>.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> is canceled, also when the
    /// body is already in memory.</exception>
    public static async Task<T> GetJsonValueAsync<T>(
        this HttpResponseMessage response, string path, CancellationToken cancellationToken = default)
        where T : IParsable<T>
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(path);
        var json = await ReadBodyAsync(response, cancellationToken).ConfigureAwait(false);
        return json.GetJsonValue<T>(path);
    }

    /// <summary>Reads the response body and reads the JSON string value at <paramref name="path"/>.
    /// See <see cref="GetJsonString(JsonElement, string)"/>.</summary>
    /// <param name="response">The HTTP response whose body is the JSON document.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket indices.</param>
    /// <param name="cancellationToken">Flows to the response-body read.</param>
    /// <returns>The string value.</returns>
    /// <exception cref="ArgumentNullException">A required argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is malformed or has a <c>[*]</c> wildcard;
    /// checked after the body is read.</exception>
    /// <exception cref="JsonExtractionException">The body is empty or not valid JSON, the path does not
    /// resolve, or the value is not a JSON string.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> is canceled, also when the
    /// body is already in memory.</exception>
    public static async Task<string> GetJsonStringAsync(
        this HttpResponseMessage response, string path, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(path);
        var json = await ReadBodyAsync(response, cancellationToken).ConfigureAwait(false);
        return json.GetJsonString(path);
    }

    /// <summary>Reads the response body and reads the JSON element at <paramref name="path"/> as a
    /// detached copy. See <see cref="GetJsonElement(JsonElement, string)"/>.</summary>
    /// <param name="response">The HTTP response whose body is the JSON document.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket indices.</param>
    /// <param name="cancellationToken">Flows to the response-body read.</param>
    /// <returns>A detached copy of the element at the path.</returns>
    /// <exception cref="ArgumentNullException">A required argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> is malformed or has a <c>[*]</c> wildcard;
    /// checked after the body is read.</exception>
    /// <exception cref="JsonExtractionException">The body is empty or not valid JSON, or the path does not
    /// resolve.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> is canceled, also when the
    /// body is already in memory.</exception>
    public static async Task<JsonElement> GetJsonElementAsync(
        this HttpResponseMessage response, string path, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(path);
        var json = await ReadBodyAsync(response, cancellationToken).ConfigureAwait(false);
        return json.GetJsonElement(path);
    }

    /// <summary>Reads the response body, throwing a <see cref="JsonExtractionException"/> when it is empty.</summary>
    /// <param name="response">The response.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The non-empty body.</returns>
    private static async Task<string> ReadBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var json = await ResponseBody.ReadAsync(response, ResponseBody.ExtractionCanceled, cancellationToken)
            .ConfigureAwait(false);
        return json.Length is 0 ? throw new JsonExtractionException(JsonFailureMessage.Sentence(ResponseBody.Empty)) : json;
    }

    /// <summary>Parses a value as <see cref="GetJsonValue{T}(JsonElement, string)"/> does: a JSON string from its
    /// text, a number or boolean from its literal, against <see cref="CultureInfo.InvariantCulture"/>.</summary>
    private static bool TryParseValue<T>(JsonElement element, [MaybeNullWhen(false)] out T value)
        where T : IParsable<T>
    {
        var raw = element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => element.GetRawText(),
            _ => null,
        };

        value = default;
        return raw is not null && T.TryParse(raw, CultureInfo.InvariantCulture, out value);
    }

    /// <summary>Checks that <paramref name="path"/> is well formed and has a <c>[*]</c> wildcard.</summary>
    private static void RequireWildcardPath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        JsonPath.Validate(path);
        if (!JsonPath.ContainsWildcard(path))
        {
            throw new ArgumentException(
                "Path '" + path + "' has no [*] wildcard, so it names one value; read it with GetJsonValue<T>.",
                nameof(path));
        }
    }

    /// <summary>Resolves <paramref name="path"/> against <paramref name="element"/>, throwing a
    /// path-context <see cref="JsonExtractionException"/> when it does not resolve, and when the element is
    /// <see langword="default"/> (it holds no JSON value).</summary>
    /// <param name="element">The element to navigate from.</param>
    /// <param name="path">The path.</param>
    /// <returns>The successful resolution.</returns>
    private static JsonPathResolution Resolve(JsonElement element, string path)
    {
        if (element.ValueKind is JsonValueKind.Undefined)
        {
            throw new JsonExtractionException(JsonFailureMessage.Sentence(JsonFailureMessage.DefaultElement));
        }

        var resolution = JsonPath.Resolve(element, path);
        if (!resolution.Found)
        {
            throw new JsonExtractionException(JsonFailureMessage.ExtractionNotFound(path, resolution));
        }

        return resolution;
    }

    /// <summary>Parses <paramref name="json"/>, mapping a malformed document to a
    /// <see cref="JsonExtractionException"/> rather than a raw <see cref="JsonException"/>.</summary>
    /// <param name="json">The JSON text.</param>
    /// <returns>The parsed document (the caller disposes it).</returns>
    private static JsonDocument ParseOrThrow(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        try
        {
            return JsonText.Parse(json);
        }
        catch (JsonException exception)
        {
            throw new JsonExtractionException(JsonFailureMessage.Sentence(JsonFailureMessage.ParseFailure(exception)), exception);
        }
    }
}
