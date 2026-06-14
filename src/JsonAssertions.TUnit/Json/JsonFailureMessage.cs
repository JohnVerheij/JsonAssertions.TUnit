using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JsonAssertions;

/// <summary>
/// Renders failure-message text for the JSON assertions. The boolean / resolution primitives
/// in <see cref="JsonPath"/> and <see cref="JsonValueComparison"/> remain authoritative for pass / fail;
/// these helpers produce the human-readable explanation surfaced in the assertion exception
/// when an assertion fails.
/// </summary>
/// <remarks>
/// <para>The point of the package over a hand-rolled <c>TryGetProperty(...).IsTrue()</c>
/// helper is that a failure says <em>where</em> on the path resolution stopped. Every
/// path-resolution failure renders a two-line block: <c>resolved as far as:</c> (the longest
/// prefix that resolved) and a reason line distinguishing "the object has no such property"
/// from "the path tried to read a property off a non-object".</para>
/// <para>Lines are terminated with the literal LF byte so the rendered text is byte-stable
/// across platforms. Numeric formatting uses <see cref="CultureInfo.InvariantCulture"/>.</para>
/// <para><strong>Public surface (extension point for consumer-authored typed JSON assertions):</strong>
/// the four path-family factories (<see cref="PropertyNotFound(string, JsonPathResolution)"/>,
/// <see cref="ValueMismatch(string, JsonPathResolution, string)"/>,
/// <see cref="ShapeMismatch(string, JsonPathResolution, string)"/>, and
/// <see cref="ParseFailure(JsonException)"/>) are part of the v0.3.0+ stable public API.
/// Consumers writing their own typed JSON assertions can compose these factories with their
/// custom <c>[GenerateAssertion]</c> methods to produce failure messages that match the
/// package's diagnostic style. Family-pattern consistency: mirrors
/// <c>MathAssertions.MathFailureMessage</c>.</para>
/// <para><strong>Internal surface:</strong> HTTP-response, ProblemDetails, and roundtrip-specific
/// factories stay <see langword="internal"/>. Those failure modes are bound to the specific
/// assertions that produce them (no extension value); keeping them internal preserves
/// future-flexibility on the message text for context-specific failures.</para>
/// <para>The exact rendered text of the public factories may evolve in minor versions
/// (added punctuation, reworded reason lines); consumers should pin tests against the
/// outcome (pass / fail) and key tokens (e.g. the path), not against full message equality.</para>
/// </remarks>
public static class JsonFailureMessage
{
    private const int MaxRenderedStringLength = 60;
    private const int MaxResponseBodyLength = 256;

    /// <summary>Renders the failure for an HTTP-response status-code mismatch: the response did
    /// not have the expected status. Includes a truncated body to aid diagnosis (a 400 response
    /// frequently carries a structured error in its body).
    /// <em>Internal: bound to <c>HasJsonResponse</c>; not part of the stable public surface.</em></summary>
    internal static string ResponseStatusMismatch(
        System.Net.HttpStatusCode expected, System.Net.HttpStatusCode actual, string body)
    {
        var sb = new StringBuilder();
        sb.Append("the response to have status ")
            .Append(Convert.ToInt32(expected, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture))
            .Append(' ').Append(expected).Append('\n');
        sb.Append("  but got: ")
            .Append(Convert.ToInt32(actual, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture))
            .Append(' ').Append(actual).Append('\n');
        if (body.Length > 0)
        {
            sb.Append("  body: ").Append(TruncateBody(body)).Append('\n');
        }
        return sb.ToString();
    }

    /// <summary>Renders the failure for an HTTP-response-body deserialization error: the status
    /// matched but the body could not be parsed via the supplied <see cref="System.Text.Json.Serialization.Metadata.JsonTypeInfo{T}"/>.
    /// <em>Internal: bound to <c>HasJsonResponse</c>; not part of the stable public surface.</em></summary>
    internal static string ResponseDeserializationFailed(string typeName, string body, JsonException exception)
    {
        var sb = new StringBuilder();
        sb.Append("the response body to deserialize as ").Append(typeName).Append('\n');
        sb.Append("  but parsing failed: ").Append(exception.Message).Append('\n');
        sb.Append("  body: ").Append(TruncateBody(body)).Append('\n');
        return sb.ToString();
    }

    /// <summary>Renders the failure for an HTTP-response-body structural-value mismatch: the
    /// status matched and the body deserialized, but the resulting value did not equal the
    /// expected one under <see cref="object.Equals(object, object)"/>.
    /// <em>Internal: bound to <c>HasJsonResponse</c>; not part of the stable public surface.</em></summary>
    internal static string ResponseValueMismatch<T>(string typeName, T? actual, T expected)
    {
        var sb = new StringBuilder();
        sb.Append("the response body to deserialize to a ").Append(typeName)
            .Append(" structurally equal to: ")
            .Append(expected?.ToString() ?? "null").Append('\n');
        sb.Append("  but got: ").Append(actual?.ToString() ?? "null").Append('\n');
        return sb.ToString();
    }

    /// <summary>Caps a response-body string so a large payload cannot bloat the message.
    /// <em>Internal helper.</em></summary>
    private static string TruncateBody(string body)
        => body.Length <= MaxResponseBodyLength
            ? body
            : body[..MaxResponseBodyLength] + "...";

    /// <summary>Renders the failure for a round-trip mismatch: the value serialized to
    /// <paramref name="json1"/>, but after deserialize-then-reserialize it produced
    /// <paramref name="json2"/>. The two strings are shown side-by-side for diagnosis.
    /// <em>Internal: bound to <c>RoundtripsCleanlyVia</c>; not part of the stable public surface.</em></summary>
    internal static string RoundtripMismatch(string json1, string json2)
    {
        var sb = new StringBuilder();
        sb.Append("the value to round-trip cleanly through the supplied JsonTypeInfo").Append('\n');
        sb.Append("  but the serialized form drifted between trips:").Append('\n');
        sb.Append("  first:  ").Append(json1).Append('\n');
        sb.Append("  second: ").Append(json2).Append('\n');
        return sb.ToString();
    }

    /// <summary>Renders the failure for a round-trip deserialization error: the first
    /// serialization succeeded but deserializing the result via the same context failed.
    /// <em>Internal: bound to <c>RoundtripsCleanlyVia</c>; not part of the stable public surface.</em></summary>
    internal static string RoundtripDeserializationFailed(string json, JsonException exception)
    {
        var sb = new StringBuilder();
        sb.Append("the value to round-trip cleanly through the supplied JsonTypeInfo").Append('\n');
        sb.Append("  but deserializing the first-pass JSON failed: ").Append(exception.Message).Append('\n');
        sb.Append("  first-pass JSON: ").Append(json).Append('\n');
        return sb.ToString();
    }

    /// <summary>Renders the failure for a round-trip that deserialized to <see langword="null"/>:
    /// the serialization succeeded but the deserialized value is null, so the second pass
    /// cannot proceed.
    /// <em>Internal: bound to <c>RoundtripsCleanlyVia</c>; not part of the stable public surface.</em></summary>
    internal static string RoundtripDeserializedToNull(string json)
    {
        var sb = new StringBuilder();
        sb.Append("the value to round-trip cleanly through the supplied JsonTypeInfo").Append('\n');
        sb.Append("  but deserializing the first-pass JSON produced null").Append('\n');
        sb.Append("  first-pass JSON: ").Append(json).Append('\n');
        return sb.ToString();
    }

    /// <summary>Renders the failure for a <see cref="JsonSerializerContext"/> that has no
    /// <see cref="System.Text.Json.Serialization.Metadata.JsonTypeInfo{T}"/> registered for the
    /// asserted type. <em>Internal: bound to <c>HasJsonTypeInfoFor</c>; not part of the stable
    /// public surface.</em></summary>
    internal static string JsonTypeInfoMissing(string typeName, string contextTypeName)
    {
        var sb = new StringBuilder();
        sb.Append("the JsonSerializerContext to have a JsonTypeInfo registered for ").Append(typeName).Append('\n');
        sb.Append("  but ").Append(contextTypeName).Append(".GetTypeInfo(typeof(")
            .Append(typeName).Append(")) returned null").Append('\n');
        sb.Append("  hint: add [JsonSerializable(typeof(").Append(typeName)
            .Append("))] to ").Append(contextTypeName).Append('\n');
        return sb.ToString();
    }

    /// <summary>Renders the failure for a response that did not return the RFC 7807
    /// <c>application/problem+json</c> Content-Type, which is required for
    /// <c>MatchesProblemDetails</c> conformance.
    /// <em>Internal: bound to <c>MatchesProblemDetails</c>; not part of the stable public surface.</em></summary>
    internal static string ProblemDetailsContentTypeMismatch(string? actualContentType, string body)
    {
        var sb = new StringBuilder();
        sb.Append("the response Content-Type to be \"application/problem+json\" (RFC 7807)").Append('\n');
        sb.Append("  but got: ").Append(actualContentType ?? "(none)").Append('\n');
        if (body.Length > 0)
        {
            sb.Append("  body: ").Append(TruncateBody(body)).Append('\n');
        }
        return sb.ToString();
    }

    /// <summary>Renders the failure for a response body that did not deserialize as
    /// ProblemDetails (typically because the body is not valid JSON, or its shape is too far
    /// off the RFC 7807 schema to satisfy STJ).
    /// <em>Internal: bound to <c>MatchesProblemDetails</c>; not part of the stable public surface.</em></summary>
    internal static string ProblemDetailsParseFailed(string body, JsonException exception)
    {
        var sb = new StringBuilder();
        sb.Append("the response body to deserialize as RFC 7807 ProblemDetails").Append('\n');
        sb.Append("  but parsing failed: ").Append(exception.Message).Append('\n');
        sb.Append("  body: ").Append(TruncateBody(body)).Append('\n');
        return sb.ToString();
    }

    /// <summary>Renders the failure for a ProblemDetails-field mismatch: at least one
    /// asserted field (status / title / detail / type / instance) did not match the value
    /// the consumer specified. All mismatched fields are reported in one message.
    /// <em>Internal: bound to <c>MatchesProblemDetails</c>; not part of the stable public surface.</em></summary>
    internal static string ProblemDetailsFieldMismatch(IReadOnlyList<(string Field, string? Expected, string? Actual)> mismatches)
    {
        var sb = new StringBuilder();
        sb.Append("the ProblemDetails to match all specified fields").Append('\n');
        foreach (var (field, expected, actual) in mismatches)
        {
            sb.Append("  ").Append(field).Append(": expected ").Append(expected ?? "null")
                .Append(", got ").Append(actual ?? "null").Append('\n');
        }
        return sb.ToString();
    }

    /// <summary>Renders the failure for a ValidationProblemDetails-errors mismatch: the
    /// errors dictionary did not contain the expected fields, or specific field-error
    /// messages did not match.
    /// <em>Internal: bound to <c>MatchesValidationProblemDetails</c>; not part of the stable public surface.</em></summary>
    internal static string ValidationErrorsMismatch(
        IReadOnlyDictionary<string, string[]> expected,
        IReadOnlyDictionary<string, string[]>? actual)
    {
        var sb = new StringBuilder();
        sb.Append("the ValidationProblemDetails errors to contain the specified entries").Append('\n');
        if (actual is null)
        {
            sb.Append("  but the response had no \"errors\" property").Append('\n');
            return sb.ToString();
        }

        foreach (var kvp in expected)
        {
            if (!actual.TryGetValue(kvp.Key, out var actualMessages))
            {
                sb.Append("  missing field \"").Append(kvp.Key).Append('"').Append('\n');
                continue;
            }
            sb.Append("  field \"").Append(kvp.Key).Append("\": expected [")
                .AppendJoin(", ", kvp.Value).Append("], got [")
                .AppendJoin(", ", actualMessages).Append(']').Append('\n');
        }
        return sb.ToString();
    }

    /// <summary>Renders the failure for a JSON <see cref="string"/> overload whose input could
    /// not be parsed at all. A malformed response body is a legitimate runtime scenario, so it
    /// is surfaced as an explained assertion failure rather than an escaping
    /// <summary>
    /// Formats a failure message for JSON parsing failure.
    /// </summary>
    /// <returns>A formatted failure message indicating the asserted value should be parseable JSON and including the parsing error.</returns>
    public static string ParseFailure(JsonException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        var sb = new StringBuilder();
        sb.Append("the asserted value to be parseable JSON").Append('\n');
        sb.Append("  but parsing failed: ").Append(exception.Message).Append('\n');
        return sb.ToString();
    }

    /// <summary>Renders the failure when the <em>expected</em> JSON supplied to an equivalence
    /// <summary>
    /// Formats an error message when the expected JSON in a test assertion cannot be parsed.
    /// </summary>
    /// <param name="exception">The JSON parsing exception that was thrown.</param>
    /// <returns>A formatted error message describing the parsing failure and including the exception details.</returns>
    public static string ExpectedParseFailure(JsonException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        var sb = new StringBuilder();
        sb.Append("the expected JSON to be parseable").Append('\n');
        sb.Append("  but parsing the expected document failed: ").Append(exception.Message).Append('\n');
        return sb.ToString();
    }

    /// <summary>Renders the failure for a structural-equivalence assertion: the path where the two
    /// documents diverge, the category of difference, and a rendered view of each side. Container
    /// values are shown as truncated raw JSON so an object/array mismatch reports its content rather
    /// <summary>
    /// Renders a failure message for a JSON structural-equivalence mismatch.
    /// </summary>
    /// <param name="difference">The structural difference describing the equivalence failure.</param>
    /// <returns>A formatted failure message indicating the location, difference reason, and expected/actual values.</returns>
    public static string EquivalenceMismatch(JsonDifference difference)
    {
        ArgumentNullException.ThrowIfNull(difference);
        var location = difference.Path.Length is 0 ? "the root" : "\"" + difference.Path + "\"";
        var sb = new StringBuilder();
        sb.Append("to be JSON-equivalent to the expected document").Append('\n');
        sb.Append("  at ").Append(location).Append(": ").Append(DescribeDifference(difference.Kind)).Append('\n');
        sb.Append("    expected: ").Append(difference.Expected).Append('\n');
        sb.Append("    actual:   ").Append(difference.Actual).Append('\n');
        return sb.ToString();
    }

    /// <summary>Maps a difference category to the one-line reason shown in the failure message.</summary>
    private static string DescribeDifference(JsonDifferenceKind kind) => kind switch
    {
        JsonDifferenceKind.Value => "value differs",
        JsonDifferenceKind.Kind => "JSON kind differs",
        JsonDifferenceKind.MissingProperty => "property missing in the actual document",
        JsonDifferenceKind.UnexpectedProperty => "unexpected property in the actual document",
        JsonDifferenceKind.ArrayLength => "array length differs",
        _ => "no equivalent element in the actual array", // ArrayElementUnmatched
    };

    /// <summary>Renders the failure for a positive property-existence assertion: the path did
    /// <summary>
    /// Renders a failure message when an expected JSON property at the specified path could not be found.
    /// </summary>
    /// <param name="resolution">The path resolution result describing how far resolution progressed and why it stopped.</param>
    /// <returns>The formatted failure message.</returns>
    public static string PropertyNotFound(string path, JsonPathResolution resolution)
    {
        ArgumentNullException.ThrowIfNull(path);
        var sb = new StringBuilder();
        sb.Append("to have a JSON property at path \"").Append(path).Append('"').Append('\n');
        AppendFailurePoint(sb, resolution);
        return sb.ToString();
    }

    /// <summary>Renders the failure for a negative property-existence assertion: the path
    /// resolved when it was asserted not to.</summary>
    public static string PropertyShouldNotExist(string path, JsonPathResolution resolution)
    {
        ArgumentNullException.ThrowIfNull(path);
        var sb = new StringBuilder();
        sb.Append("to have no JSON property at path \"").Append(path).Append('"').Append('\n');
        sb.Append("  but ").Append(DescribeKind(resolution.Element.ValueKind))
            .Append(" exists at that path").Append('\n');
        return sb.ToString();
    }

    /// <summary>Renders the failure for a value-at-path assertion. When the path itself did
    /// not resolve, the failure-point block is appended; when it resolved but the value did
    /// not match, the found value is shown against the expected one.</summary>
    public static string ValueMismatch(string path, JsonPathResolution resolution, string expectedDescription)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(expectedDescription);
        var sb = new StringBuilder();
        sb.Append("to have JSON value ").Append(expectedDescription)
            .Append(" at path \"").Append(path).Append('"').Append('\n');

        if (resolution.Found)
        {
            sb.Append("  found: ").Append(RenderElement(resolution.Element)).Append('\n');
        }
        else
        {
            AppendFailurePoint(sb, resolution);
        }

        return sb.ToString();
    }

    /// <summary>Renders the failure for a shape assertion (array length, non-empty / empty
    /// array, value kind). When the path did not resolve, the failure-point block is appended;
    /// when it resolved but the element has the wrong shape, the found shape is shown.</summary>
    public static string ShapeMismatch(string path, JsonPathResolution resolution, string expectedDescription)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(expectedDescription);
        var sb = new StringBuilder();
        sb.Append("to have ").Append(expectedDescription)
            .Append(" at path \"").Append(path).Append('"').Append('\n');

        if (resolution.Found)
        {
            sb.Append("  found: ").Append(RenderShape(resolution.Element)).Append('\n');
        }
        else
        {
            AppendFailurePoint(sb, resolution);
        }

        return sb.ToString();
    }

    /// <summary>Appends the two-line "resolved as far as / reason" block for a failed
    /// resolution. The reason line distinguishes four failure modes: a missing property on an
    /// object, a property-access on a non-object, an out-of-range index on an array, and an
    /// index-access on a non-array.</summary>
    private static void AppendFailurePoint(StringBuilder sb, JsonPathResolution resolution)
    {
        var prefix = resolution.ResolvedPrefix.Length is 0 ? "(root)" : resolution.ResolvedPrefix;
        sb.Append("  resolved as far as: ").Append(prefix).Append('\n');

        var location = resolution.ResolvedPrefix.Length is 0
            ? "the root"
            : "\"" + resolution.ResolvedPrefix + "\"";

        // FailedSegment is non-null on failed resolutions, which is the only path that
        // reaches AppendFailurePoint (the Found=true branches short-circuit before this call).
        var failedSegment = resolution.FailedSegment!;
        var isIndex = failedSegment.StartsWith('[') && failedSegment.EndsWith(']');

        if (isIndex)
        {
            if (resolution.ContainerKind is JsonValueKind.Array)
            {
                sb.Append("  no element at index ").Append(failedSegment)
                    .Append(" on ").Append(location).Append('\n');
            }
            else
            {
                sb.Append("  cannot index ").Append(failedSegment)
                    .Append(": ").Append(location).Append(" is ")
                    .Append(DescribeKind(resolution.ContainerKind)).Append(", not an array").Append('\n');
            }

            return;
        }

        if (resolution.ContainerKind is JsonValueKind.Object)
        {
            sb.Append("  no property \"").Append(failedSegment)
                .Append("\" on ").Append(location).Append('\n');
        }
        else
        {
            sb.Append("  cannot read property \"").Append(failedSegment)
                .Append("\": ").Append(location).Append(" is ")
                .Append(DescribeKind(resolution.ContainerKind)).Append(", not an object").Append('\n');
        }
    }

    /// <summary>Renders a resolved element compactly: scalars by their raw JSON text, objects
    /// and arrays by kind only (their full text would bloat the message).</summary>
    private static string RenderElement(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => "an object",
        JsonValueKind.Array => "an array",
        JsonValueKind.String => "\"" + Truncate(element.GetString()!) + "\"",
        JsonValueKind.Number => element.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Null => "null",
        _ => "(undefined)",
    };

    /// <summary>Renders a resolved element for a shape failure: an array reports its length,
    /// any other kind reports the kind alone.</summary>
    private static string RenderShape(JsonElement element)
        => element.ValueKind is JsonValueKind.Array
            ? "an array of length " + element.GetArrayLength().ToString(CultureInfo.InvariantCulture)
            : DescribeKind(element.ValueKind);

    /// <summary>The indefinite-article kind description used in failure reasons.</summary>
    private static string DescribeKind(JsonValueKind kind) => kind switch
    {
        JsonValueKind.Object => "an Object",
        JsonValueKind.Array => "an Array",
        JsonValueKind.String => "a String",
        JsonValueKind.Number => "a Number",
        JsonValueKind.True or JsonValueKind.False => "a Boolean",
        JsonValueKind.Null => "a Null",
        _ => "an undefined value",
    };

    /// <summary>Caps a rendered string value so a large payload cannot bloat the message.</summary>
    private static string Truncate(string value)
        => value.Length <= MaxRenderedStringLength
            ? value
            : value[..MaxRenderedStringLength] + "...";
}
