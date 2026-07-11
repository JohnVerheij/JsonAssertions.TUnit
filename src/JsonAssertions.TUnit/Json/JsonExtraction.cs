using System;
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
/// assertions; the value they return is used directly.
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
    /// <exception cref="JsonExtractionException">The path does not resolve, or the value does not
    /// parse as <typeparamref name="T"/>.</exception>
    public static T GetJsonValue<T>(this JsonElement element, string path)
        where T : IParsable<T>
    {
        ArgumentNullException.ThrowIfNull(path);
        var resolution = Resolve(element, path);

        var raw = resolution.Element.ValueKind switch
        {
            JsonValueKind.String => resolution.Element.GetString(),
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => resolution.Element.GetRawText(),
            _ => null,
        };

        if (raw is not null && T.TryParse(raw, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        throw new JsonExtractionException(
            JsonFailureMessage.ValueMismatch(path, resolution, "a JSON value parseable as " + typeof(T).Name));
    }

    /// <summary>Parses <paramref name="json"/> and reads the value at <paramref name="path"/> as
    /// <typeparamref name="T"/>. See <see cref="GetJsonValue{T}(JsonElement, string)"/>.</summary>
    /// <typeparam name="T">The target type implementing <see cref="IParsable{T}"/>.</typeparam>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket indices.</param>
    /// <returns>The parsed value.</returns>
    /// <exception cref="ArgumentNullException">A required argument is <see langword="null"/>.</exception>
    /// <exception cref="JsonExtractionException">The text is not valid JSON, the path does not resolve,
    /// or the value does not parse as <typeparamref name="T"/>.</exception>
    public static T GetJsonValue<T>(this string json, string path)
        where T : IParsable<T>
    {
        using var document = ParseOrThrow(json);
        return document.RootElement.GetJsonValue<T>(path);
    }

    /// <summary>Reads the JSON string value at <paramref name="path"/>. The value must be a JSON
    /// string (a number, boolean, object, or array is a failure; use <see cref="GetJsonValue{T}(JsonElement, string)"/>
    /// or <see cref="GetJsonElement(JsonElement, string)"/> for those).</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket indices.</param>
    /// <returns>The string value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
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
            JsonFailureMessage.ValueMismatch(path, resolution, "a JSON string"));
    }

    /// <summary>Parses <paramref name="json"/> and reads the JSON string value at
    /// <paramref name="path"/>. See <see cref="GetJsonString(JsonElement, string)"/>.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket indices.</param>
    /// <returns>The string value.</returns>
    /// <exception cref="ArgumentNullException">A required argument is <see langword="null"/>.</exception>
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
    /// <exception cref="JsonExtractionException">The body is not valid JSON, the path does not resolve,
    /// or the value does not parse as <typeparamref name="T"/>.</exception>
    public static async Task<T> GetJsonValueAsync<T>(
        this HttpResponseMessage response, string path, CancellationToken cancellationToken = default)
        where T : IParsable<T>
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(path);
        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return json.GetJsonValue<T>(path);
    }

    /// <summary>Reads the response body and reads the JSON string value at <paramref name="path"/>.
    /// See <see cref="GetJsonString(JsonElement, string)"/>.</summary>
    /// <param name="response">The HTTP response whose body is the JSON document.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket indices.</param>
    /// <param name="cancellationToken">Flows to the response-body read.</param>
    /// <returns>The string value.</returns>
    /// <exception cref="ArgumentNullException">A required argument is <see langword="null"/>.</exception>
    /// <exception cref="JsonExtractionException">The body is not valid JSON, the path does not resolve,
    /// or the value is not a JSON string.</exception>
    public static async Task<string> GetJsonStringAsync(
        this HttpResponseMessage response, string path, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(path);
        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return json.GetJsonString(path);
    }

    /// <summary>Reads the response body and reads the JSON element at <paramref name="path"/> as a
    /// detached copy. See <see cref="GetJsonElement(JsonElement, string)"/>.</summary>
    /// <param name="response">The HTTP response whose body is the JSON document.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket indices.</param>
    /// <param name="cancellationToken">Flows to the response-body read.</param>
    /// <returns>A detached copy of the element at the path.</returns>
    /// <exception cref="ArgumentNullException">A required argument is <see langword="null"/>.</exception>
    /// <exception cref="JsonExtractionException">The body is not valid JSON, or the path does not resolve.</exception>
    public static async Task<JsonElement> GetJsonElementAsync(
        this HttpResponseMessage response, string path, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(path);
        var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return json.GetJsonElement(path);
    }

    /// <summary>Resolves <paramref name="path"/> against <paramref name="element"/>, throwing a
    /// path-context <see cref="JsonExtractionException"/> when it does not resolve.</summary>
    /// <param name="element">The element to navigate from.</param>
    /// <param name="path">The path.</param>
    /// <returns>The successful resolution.</returns>
    private static JsonPathResolution Resolve(JsonElement element, string path)
    {
        var resolution = JsonPath.Resolve(element, path);
        if (!resolution.Found)
        {
            throw new JsonExtractionException(JsonFailureMessage.PropertyNotFound(path, resolution));
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
            return JsonDocument.Parse(json);
        }
        catch (JsonException exception)
        {
            throw new JsonExtractionException(JsonFailureMessage.ParseFailure(exception), exception);
        }
    }
}
