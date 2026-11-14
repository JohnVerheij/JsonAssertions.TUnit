using System;
using System.Collections.Generic;
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
/// helper is that a failure says <em>where</em> on the path resolution stopped: the longest prefix
/// that resolved and why the next segment did not (no such property, or a value of the wrong kind),
/// as in <c>resolved as far as 'user', which has no property 'email'</c>.</para>
/// <para><strong>The message rule.</strong> TUnit prints the expectation from the assertion's
/// <c>ExpectationMessage</c> (<c>Expected to have a JSON property at path 'user.email'</c>), and every
/// factory here returns only the text after <c>but</c>: what was found. A value the expectation
/// cannot show appears once, in parentheses (<c>found 31 (expected 30.5)</c>). Names are in single
/// quotes and values are written as JSON literals; a string placeholder in an expectation is in
/// single quotes as well, because TUnit's generator pastes <c>ExpectationMessage</c> into an
/// interpolated string that cannot hold a double quote. The texts have no trailing newline, a
/// multi-line text joins its lines with LF so it is byte-stable across platforms, and numbers use
/// <see cref="CultureInfo.InvariantCulture"/>.</para>
/// <para><strong>Public surface (extension point for consumer-authored typed JSON assertions):</strong>
/// the five path-family factories (<see cref="PropertyNotFound(string, JsonPathResolution)"/>,
/// <see cref="PropertyShouldNotExist(string, JsonPathResolution)"/>,
/// <see cref="ValueMismatch(string, JsonPathResolution, string)"/>,
/// <see cref="ShapeMismatch(string, JsonPathResolution, string)"/>, and
/// <see cref="ParseFailure(JsonException)"/>) are part of the v0.3.0+ stable public API.
/// A consumer's own <c>[GenerateAssertion]</c> method states its expectation in
/// <c>ExpectationMessage</c> (for example <c>"to have order status '{expectedStatus}'"</c>) and
/// returns one of these factories' texts, so its failures read like the package's. Family-pattern
/// consistency: mirrors <c>MathAssertions.MathFailureMessage</c>.</para>
/// <para>The equivalence and subset assertions add three more public factories:
/// <see cref="ExpectedParseFailure(JsonException)"/>,
/// <see cref="EquivalenceMismatch(JsonDifference)"/>, and
/// <see cref="ContainsMismatch(IReadOnlyList{JsonDifference})"/>.</para>
/// <para><strong>Internal surface:</strong> HTTP-response, ProblemDetails, extraction and roundtrip-specific
/// factories stay <see langword="internal"/>. Those failure modes are bound to the specific
/// assertions that produce them (no extension value); keeping them internal preserves
/// future-flexibility on the message text for context-specific failures.</para>
/// <para>The exact rendered text of the public factories may evolve in minor versions
/// (added punctuation, reworded reason lines); consumers should pin tests against the
/// outcome (pass / fail) and key tokens (e.g. the path), not against full message equality.
/// 0.7.0 changed every text to the message rule above.</para>
/// </remarks>
public static class JsonFailureMessage
{
    private const int MaxRenderedStringLength = 60;
    private const int MaxResponseBodyLength = 256;

    /// <summary>The failure for a <see langword="default"/> <see cref="JsonElement"/>, which holds no JSON value.
    /// <em>Internal: bound to the element assertions and the extraction methods.</em></summary>
    internal const string DefaultElement = "the JsonElement is default: it holds no JSON value";

    /// <summary>Renders the failure for a JSON <see cref="string"/> overload whose input could
    /// not be parsed at all. A malformed response body is a legitimate runtime scenario, so it
    /// is surfaced as an explained assertion failure rather than an escaping
    /// <see cref="JsonException"/>.</summary>
    /// <param name="exception">The parse failure.</param>
    /// <returns>The rendered failure, such as <c>the value is not valid JSON: …</c>.</returns>
    public static string ParseFailure(JsonException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return "the value is not valid JSON: " + exception.Message;
    }

    /// <summary>Renders the failure when the <em>expected</em> JSON supplied to an equivalence
    /// assertion is itself malformed (a test-authoring error rather than a value mismatch).</summary>
    /// <param name="exception">The parse failure of the expected document.</param>
    /// <returns>The rendered failure, such as <c>the expected document is not valid JSON: …</c>.</returns>
    public static string ExpectedParseFailure(JsonException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return "the expected document is not valid JSON: " + exception.Message;
    }

    /// <summary>Renders the failure for a structural-equivalence assertion: the path where the two
    /// documents diverge, the category of difference, and a rendered view of each side. Container
    /// values are shown as truncated raw JSON so an object/array mismatch reports its content rather
    /// than just its kind.</summary>
    /// <param name="difference">The first difference found.</param>
    /// <returns>The rendered failure, such as <c>at 'a' the value differs</c> with an expected and an actual line.</returns>
    public static string EquivalenceMismatch(JsonDifference difference)
    {
        ArgumentNullException.ThrowIfNull(difference);
        var sb = new StringBuilder();
        AppendDifference(sb, difference, indent: "  ");
        return sb.ToString();
    }

    /// <summary>Renders the failure for a subset (contains) assertion: every path where the actual
    /// document does not contain the expected value, each with the category of difference and a
    /// rendered view of both sides. Unlike <see cref="EquivalenceMismatch(JsonDifference)"/>, all
    /// differences are listed in one message (subset assertions replace a block of per-field checks,
    /// so a single run must enumerate every field that is wrong).</summary>
    /// <param name="differences">The differences found; must be non-empty.</param>
    /// <returns>The rendered failure, such as <c>2 paths differ:</c> followed by each difference.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="differences"/> is <see langword="null"/>.</exception>
    public static string ContainsMismatch(IReadOnlyList<JsonDifference> differences)
    {
        ArgumentNullException.ThrowIfNull(differences);
        var sb = new StringBuilder();
        sb.Append(CountOf(differences.Count, "path")).Append(differences.Count is 1 ? " differs:" : " differ:");
        foreach (var difference in differences)
        {
            sb.Append("\n  ");
            AppendDifference(sb, difference, indent: "    ");
        }

        return sb.ToString();
    }

    /// <summary>Renders the failure for a positive property-existence assertion: where the path stopped resolving
    /// and why.</summary>
    /// <param name="path">The asserted path; the assertion's expectation names it.</param>
    /// <param name="resolution">A failed resolution of <paramref name="path"/>, as <see cref="JsonPath.Resolve"/>
    /// returns it.</param>
    /// <returns>The rendered failure, such as <c>resolved as far as 'user', which has no property 'email'</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="resolution"/> is found, or is a <see langword="default"/>
    /// resolution.</exception>
    public static string PropertyNotFound(string path, JsonPathResolution resolution)
    {
        ArgumentNullException.ThrowIfNull(path);
        RequireFailed(resolution, nameof(PropertyNotFound));
        return DescribeFailure(resolution);
    }

    /// <summary>Renders the failure for a negative property-existence assertion: the value found where none was
    /// expected.</summary>
    /// <param name="path">The asserted path; the assertion's expectation names it.</param>
    /// <param name="resolution">A found resolution of <paramref name="path"/>.</param>
    /// <returns>The rendered failure, such as <c>found "alice"</c> or <c>found an Object</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="resolution"/> is not found.</exception>
    public static string PropertyShouldNotExist(string path, JsonPathResolution resolution)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (!resolution.Found)
        {
            throw new ArgumentException(
                "PropertyShouldNotExist renders a resolution that was found; this one was not.", nameof(resolution));
        }

        return "found " + RenderElement(resolution.Element);
    }

    /// <summary>Renders the failure for a value-at-path assertion: the value found, or where the path stopped
    /// resolving.</summary>
    /// <param name="path">The asserted path; the assertion's expectation names it.</param>
    /// <param name="resolution">The resolution of <paramref name="path"/>, found or failed.</param>
    /// <param name="expectedDescription">The expected value. Kept for compatibility and not rendered since 0.7.0: the
    /// assertion's <c>ExpectationMessage</c> states the expectation.</param>
    /// <returns>The rendered failure, such as <c>found "alice"</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> or <paramref name="expectedDescription"/> is
    /// <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="resolution"/> is a <see langword="default"/> resolution.</exception>
    public static string ValueMismatch(string path, JsonPathResolution resolution, string expectedDescription)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(expectedDescription);
        RequireResolved(resolution, nameof(ValueMismatch));
        return Found(resolution);
    }

    /// <summary>Renders the failure for a shape assertion (array length, non-empty / empty array, value kind): the
    /// shape found, or where the path stopped resolving.</summary>
    /// <param name="path">The asserted path; the assertion's expectation names it.</param>
    /// <param name="resolution">The resolution of <paramref name="path"/>, found or failed.</param>
    /// <param name="expectedDescription">The expected shape. Kept for compatibility and not rendered since 0.7.0: the
    /// assertion's <c>ExpectationMessage</c> states the expectation.</param>
    /// <returns>The rendered failure, such as <c>found an array of length 2</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> or <paramref name="expectedDescription"/> is
    /// <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="resolution"/> is a <see langword="default"/> resolution.</exception>
    public static string ShapeMismatch(string path, JsonPathResolution resolution, string expectedDescription)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(expectedDescription);
        RequireResolved(resolution, nameof(ShapeMismatch));
        return FoundShape(resolution);
    }

    /// <summary>The value found, such as <c>found "alice"</c>, or where the path stopped resolving.</summary>
    /// <param name="resolution">A resolution from <see cref="JsonPath"/>.</param>
    /// <returns>The description.</returns>
    internal static string Found(JsonPathResolution resolution)
        => resolution.Found ? "found " + RenderElement(resolution.Element) : DescribeFailure(resolution);

    /// <summary>The value found with a parenthesized <paramref name="trailer"/> for what the expectation cannot show,
    /// such as <c>found 31 (expected 30.5)</c>, or where the path stopped resolving.</summary>
    /// <param name="resolution">A resolution from <see cref="JsonPath"/>.</param>
    /// <param name="trailer">The text inside the parentheses.</param>
    /// <returns>The description.</returns>
    internal static string Found(JsonPathResolution resolution, string trailer)
        => resolution.Found ? Found(resolution) + " (" + trailer + ")" : DescribeFailure(resolution);

    /// <summary>The value found for an integer expectation; a JSON string that is not a JSON integer literal says
    /// so, such as <c>found "+42", which is not a JSON integer literal</c>.</summary>
    /// <param name="resolution">A resolution from <see cref="JsonPath"/>.</param>
    /// <returns>The description.</returns>
    internal static string FoundInteger(JsonPathResolution resolution)
        => WithIntegerNote(resolution, Found(resolution));

    /// <summary>The value found for an integer one-of expectation, with the integer note and a parenthesized
    /// <paramref name="trailer"/>, such as <c>found "+42", which is not a JSON integer literal (candidates: 42)</c>, or
    /// where the path stopped resolving.</summary>
    /// <param name="resolution">A resolution from <see cref="JsonPath"/>.</param>
    /// <param name="trailer">The text inside the parentheses.</param>
    /// <returns>The description.</returns>
    internal static string FoundInteger(JsonPathResolution resolution, string trailer)
        => resolution.Found
            ? WithIntegerNote(resolution, Found(resolution)) + " (" + trailer + ")"
            : DescribeFailure(resolution);

    /// <summary>The shape found, such as <c>found an array of length 2</c>, or where the path stopped
    /// resolving.</summary>
    /// <param name="resolution">A resolution from <see cref="JsonPath"/>.</param>
    /// <returns>The description.</returns>
    internal static string FoundShape(JsonPathResolution resolution)
        => resolution.Found ? "found " + RenderShape(resolution.Element) : DescribeFailure(resolution);

    /// <summary>Describes a found value that does not parse as the requested type, such as
    /// <c>found 7, which does not parse as Guid</c>.</summary>
    /// <param name="element">The found value.</param>
    /// <param name="typeName">The requested type's name.</param>
    /// <returns>The description.</returns>
    internal static string ValueNotParsed(JsonElement element, string typeName)
        => "found " + RenderElement(element) + ", which does not parse as " + typeName;

    /// <summary>Describes in one line where a failed resolution stopped and why, such as
    /// <c>resolved as far as 'user', which has no property 'email'</c>.</summary>
    /// <param name="resolution">A failed resolution from <see cref="JsonPath"/>.</param>
    /// <returns>The description.</returns>
    internal static string DescribeFailure(JsonPathResolution resolution)
    {
        var location = resolution.ResolvedPrefix.Length is 0 ? "the root" : "'" + resolution.ResolvedPrefix + "'";
        // The public factories refuse a default resolution, and a failed resolution from JsonPath always carries its
        // failed segment, so FailedSegment is set here.
        var segment = resolution.FailedSegment!;
        var kind = DescribeKind(resolution.ContainerKind);
        if (string.Equals(segment, "[*]", StringComparison.Ordinal))
        {
            return "cannot expand [*]: " + location + " is " + kind + ", not an array";
        }

        string reason;
        if (segment.AsSpan().StartsWith('['))
        {
            reason = resolution.ContainerKind is JsonValueKind.Array
                ? "has " + CountOf(resolution.ContainerLength, "element") + ", so no index " + segment
                : "is " + kind + ", not an array";
        }
        else
        {
            reason = resolution.ContainerKind is JsonValueKind.Object
                ? "has no property '" + segment + "'"
                : "is " + kind + ", not an object";
        }

        return "resolved as far as " + location + ", which " + reason;
    }

    /// <summary>The concrete location where a failed resolution stopped: the resolved prefix with the failed segment,
    /// such as <c>entries[2].name</c>.</summary>
    /// <param name="resolution">A failed resolution from <see cref="JsonPath"/>.</param>
    /// <returns>The location.</returns>
    internal static string FailedLocation(JsonPathResolution resolution)
    {
        var segment = resolution.FailedSegment!;
        return resolution.ResolvedPrefix.Length is 0 || segment.AsSpan().StartsWith('[')
            ? resolution.ResolvedPrefix + segment
            : resolution.ResolvedPrefix + "." + segment;
    }

    /// <summary>Renders the failure of an assertion over several paths: every path that did not resolve, in the given
    /// order, each with where its resolution stopped; a <c>[*]</c> path shows its first failing element and how many of
    /// its elements failed.
    /// <em>Internal: bound to <c>HasJsonProperties</c>; not part of the stable public surface.</em></summary>
    /// <param name="failures">Each path that did not resolve, with its first failed resolution, the number of its
    /// resolutions that failed, and the number it expanded to.</param>
    /// <param name="pathCount">The number of asserted paths.</param>
    /// <returns>The rendered failure.</returns>
    internal static string PropertiesNotFound(
        IReadOnlyList<(string Path, JsonPathResolution Failure, int Failed, int Total)> failures, int pathCount)
    {
        var sb = new StringBuilder();
        sb.Append(failures.Count.ToString(CultureInfo.InvariantCulture)).Append(" of ")
            .Append(pathCount.ToString(CultureInfo.InvariantCulture)).Append(pathCount is 1 ? " path" : " paths")
            .Append(" did not resolve:");
        foreach (var (path, failure, failed, total) in failures)
        {
            sb.Append("\n  '").Append(path).Append("': ").Append(DescribeFailure(failure));
            if (total > 1)
            {
                sb.Append(" (").Append(failed.ToString(CultureInfo.InvariantCulture)).Append(" of ")
                    .Append(total.ToString(CultureInfo.InvariantCulture)).Append(" elements)");
            }
        }

        return sb.ToString();
    }

    /// <summary>Renders the failure of reading every value a wildcard path expands to: how many of them could not be
    /// read, then each one on its own line.
    /// <em>Internal: bound to <c>GetJsonValues</c>; not part of the stable public surface.</em></summary>
    /// <param name="path">The wildcard path.</param>
    /// <param name="failures">One line per value that could not be read, in document order.</param>
    /// <param name="total">The number of values the path expanded to.</param>
    /// <returns>The rendered failure.</returns>
    internal static string ValuesNotRead(string path, IReadOnlyList<string> failures, int total)
    {
        var sb = new StringBuilder();
        sb.Append("Could not read ").Append(failures.Count.ToString(CultureInfo.InvariantCulture)).Append(" of ")
            .Append(CountOf(total, "value")).Append(" at '").Append(path).Append("':");
        foreach (var failure in failures)
        {
            sb.Append("\n  ").Append(failure);
        }

        return sb.ToString();
    }

    /// <summary>The message of an extraction whose path does not resolve.
    /// <em>Internal: bound to the extraction methods.</em></summary>
    /// <param name="path">The path.</param>
    /// <param name="resolution">Its failed resolution.</param>
    /// <returns>The message, such as <c>No JSON property at path 'user.nick': resolved as far as 'user', …</c>.</returns>
    internal static string ExtractionNotFound(string path, JsonPathResolution resolution)
        => "No JSON property at path '" + path + "': " + DescribeFailure(resolution);

    /// <summary>The message of an extraction whose value is not what the method reads.
    /// <em>Internal: bound to the extraction methods.</em></summary>
    /// <param name="path">The path.</param>
    /// <param name="element">The value found there.</param>
    /// <param name="problem">Why it cannot be read, such as <c>which does not parse as Int32</c>.</param>
    /// <returns>The message, such as <c>The value at 'user.name' is "alice", which does not parse as Int32</c>.</returns>
    internal static string ExtractionUnreadable(string path, JsonElement element, string problem)
        => "The value at '" + path + "' is " + RenderElement(element) + ", " + problem;

    /// <summary>Capitalizes a failure text for use as an exception message, which has no expectation before it.</summary>
    /// <param name="text">The failure text.</param>
    /// <returns>The text with its first letter in upper case.</returns>
    internal static string Sentence(string text)
        => string.Concat(char.ToUpperInvariant(text[0]).ToString(), text.AsSpan(1));

    /// <summary>Renders the failure for an HTTP-response status-code mismatch, with a truncated body to aid
    /// diagnosis (a 400 response frequently carries a structured error in its body).
    /// <em>Internal: bound to <c>HasJsonResponse</c>; not part of the stable public surface.</em></summary>
    /// <param name="actual">The status of the response.</param>
    /// <param name="body">The body, empty when there is none.</param>
    /// <returns>The rendered failure, such as <c>the status was 400 BadRequest; body: {"error":"x"}</c>.</returns>
    internal static string ResponseStatusMismatch(System.Net.HttpStatusCode actual, string body)
        => "the status was " + RenderStatusCode(actual) + BodySuffix(body);

    /// <summary>Renders the failure for an HTTP-response-body deserialization error: the status matched but the body
    /// could not be parsed via the supplied <see cref="System.Text.Json.Serialization.Metadata.JsonTypeInfo{T}"/>.
    /// <em>Internal: bound to <c>HasJsonResponse</c>; not part of the stable public surface.</em></summary>
    /// <param name="typeName">The name of the expected type.</param>
    /// <param name="body">The body.</param>
    /// <param name="exception">The deserialization failure.</param>
    /// <returns>The rendered failure.</returns>
    internal static string ResponseDeserializationFailed(string typeName, string body, JsonException exception)
        => "the body did not deserialize as " + typeName + ": " + exception.Message + BodySuffix(body);

    /// <summary>Renders the failure for an HTTP-response-body value mismatch: the status matched and the body
    /// deserialized, to a value not equal to the expected one under <see cref="object.Equals(object, object)"/>.
    /// <em>Internal: bound to <c>HasJsonResponse</c>; not part of the stable public surface.</em></summary>
    /// <typeparam name="T">The expected type.</typeparam>
    /// <param name="actual">The deserialized value.</param>
    /// <returns>The rendered failure, such as <c>the body deserialized to TestDto { Id = 2, Name = b }</c>.</returns>
    internal static string ResponseValueMismatch<T>(T? actual)
        => "the body deserialized to " + RenderObject(actual);

    /// <summary>Renders the failure for a round-trip mismatch: the second serialization differs from the first.
    /// <em>Internal: bound to <c>RoundtripsCleanlyVia</c>; not part of the stable public surface.</em></summary>
    /// <param name="json1">The first serialization.</param>
    /// <param name="json2">The serialization after the round trip.</param>
    /// <returns>The rendered failure.</returns>
    internal static string RoundtripMismatch(string json1, string json2)
        => "the second serialization differed:\n  first:  " + json1 + "\n  second: " + json2;

    /// <summary>Renders the failure for a round-trip deserialization error: the first serialization did not
    /// deserialize through the same <see cref="System.Text.Json.Serialization.Metadata.JsonTypeInfo{T}"/>.
    /// <em>Internal: bound to <c>RoundtripsCleanlyVia</c>; not part of the stable public surface.</em></summary>
    /// <param name="json">The first serialization.</param>
    /// <param name="exception">The deserialization failure.</param>
    /// <returns>The rendered failure.</returns>
    internal static string RoundtripDeserializationFailed(string json, JsonException exception)
        => "the first serialization did not deserialize: " + exception.Message + "\n  first:  " + json;

    /// <summary>Renders the failure for a round trip whose first serialization deserialized to
    /// <see langword="null"/>, so there is no second serialization.
    /// <em>Internal: bound to <c>RoundtripsCleanlyVia</c>; not part of the stable public surface.</em></summary>
    /// <param name="json">The first serialization.</param>
    /// <returns>The rendered failure.</returns>
    internal static string RoundtripDeserializedToNull(string json)
        => "the first serialization deserialized to null\n  first:  " + json;

    /// <summary>Renders the failure for a <see cref="JsonSerializerContext"/> that has no
    /// <see cref="System.Text.Json.Serialization.Metadata.JsonTypeInfo{T}"/> registered for the
    /// asserted type. <em>Internal: bound to <c>HasJsonTypeInfoFor</c>; not part of the stable
    /// public surface.</em></summary>
    /// <param name="typeName">The asserted type's name.</param>
    /// <param name="contextTypeName">The context's type name.</param>
    /// <returns>The rendered failure, with the attribute to add.</returns>
    internal static string JsonTypeInfoMissing(string typeName, string contextTypeName)
        => contextTypeName + ".GetTypeInfo(typeof(" + typeName + ")) returned null; add [JsonSerializable(typeof("
           + typeName + "))] to " + contextTypeName;

    /// <summary>Renders the failure for a response that did not return the RFC 7807
    /// <c>application/problem+json</c> Content-Type.
    /// <em>Internal: bound to the two ProblemDetails assertions; not part of the stable public surface.</em></summary>
    /// <param name="actualContentType">The media type of the response, or <see langword="null"/> when it has none.</param>
    /// <param name="body">The body.</param>
    /// <returns>The rendered failure, such as <c>the Content-Type was application/json, not application/problem+json;
    /// body: …</c>.</returns>
    internal static string ProblemDetailsContentTypeMismatch(string? actualContentType, string body)
        => "the Content-Type was " + (actualContentType ?? "absent") + ", not application/problem+json" + BodySuffix(body);

    /// <summary>Renders the failure for a response body that did not deserialize as ProblemDetails (typically
    /// because the body is not valid JSON, or its shape is too far off the RFC 7807 schema to satisfy STJ).
    /// <em>Internal: bound to the two ProblemDetails assertions; not part of the stable public surface.</em></summary>
    /// <param name="body">The body.</param>
    /// <param name="exception">The deserialization failure.</param>
    /// <returns>The rendered failure.</returns>
    internal static string ProblemDetailsParseFailed(string body, JsonException exception)
        => "the body did not deserialize as ProblemDetails: " + exception.Message + BodySuffix(body);

    /// <summary>Renders the failure of a ProblemDetails assertion: every entry that differed, each as what was found
    /// and, in parentheses, what was expected, for example <c>status: 400 (expected 422)</c>. An entry found absent
    /// (<c>Actual</c> is <see langword="null"/>) renders as <c>absent</c>; an entry with no single expected value
    /// (<c>Expected</c> is <see langword="null"/>) has no parenthesis.
    /// <em>Internal: bound to the two ProblemDetails assertions; not part of the stable public surface.</em></summary>
    /// <param name="mismatches">The entries that differed, with their rendered values; at least one.</param>
    /// <param name="singular">The noun for one entry, such as <c>field</c>.</param>
    /// <param name="plural">The noun for several entries, such as <c>fields</c>.</param>
    /// <returns>The rendered failure.</returns>
    internal static string ProblemDetailsMismatch(
        IReadOnlyList<(string Field, string? Expected, string? Actual)> mismatches, string singular, string plural)
    {
        var sb = new StringBuilder();
        sb.Append(mismatches.Count.ToString(CultureInfo.InvariantCulture)).Append(' ')
            .Append(mismatches.Count is 1 ? singular : plural).Append(" differed:");
        foreach (var (field, expected, actual) in mismatches)
        {
            sb.Append("\n  ").Append(field).Append(": ").Append(actual ?? "absent");
            if (expected is not null)
            {
                sb.Append(" (expected ").Append(expected).Append(')');
            }
        }

        return sb.ToString();
    }

    /// <summary>Renders an HTTP status code as its number and, when it has one, its name, such as <c>200 OK</c>.</summary>
    /// <param name="code">The status code.</param>
    /// <returns>The rendered status code.</returns>
    internal static string RenderStatusCode(System.Net.HttpStatusCode code)
    {
        var number = Convert.ToInt32(code, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
        return Enum.IsDefined(code) ? number + " " + code : number;
    }

    /// <summary>Renders validation messages as a JSON array of strings, such as <c>["too low"]</c>; a
    /// <see langword="null"/> array, read from a JSON null, renders as the JSON literal for it.</summary>
    /// <param name="messages">The messages.</param>
    /// <returns>The rendered messages.</returns>
    internal static string RenderMessages(string[]? messages)
        => messages is null ? "null" : "[" + string.Join(", ", Array.ConvertAll(messages, Quote)) + "]";

    /// <summary>Renders <paramref name="value"/> as a JSON string literal: in double quotes, with <c>"</c>,
    /// <c>\</c> and the ASCII control characters escaped, so a value that contains them renders unambiguously.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The quoted value.</returns>
    internal static string Quote(string value)
    {
        var sb = new StringBuilder(value.Length + 2).Append('"');
        foreach (var ch in value)
        {
            _ = ch switch
            {
                '\\' => sb.Append("\\\\"),
                '"' => sb.Append("\\\""),
                '\b' => sb.Append("\\b"),
                '\f' => sb.Append("\\f"),
                '\n' => sb.Append("\\n"),
                '\r' => sb.Append("\\r"),
                '\t' => sb.Append("\\t"),
                < ' ' => sb.Append("\\u").Append(Convert.ToInt32(ch).ToString("x4", CultureInfo.InvariantCulture)),
                _ => sb.Append(ch),
            };
        }

        return sb.Append('"').ToString();
    }

    /// <summary>Appends one difference: its location and category, then the expected and the actual side, each on
    /// its own line with <paramref name="indent"/>. An unmatched array element has no actual side: the category
    /// already says that nothing in the actual array matched.</summary>
    private static void AppendDifference(StringBuilder sb, JsonDifference difference, string indent)
    {
        var location = difference.Path.Length is 0 ? "the root" : "'" + difference.Path + "'";
        sb.Append("at ").Append(location).Append(' ').Append(DescribeDifference(difference.Kind));
        sb.Append('\n').Append(indent).Append("expected: ").Append(difference.Expected);
        if (difference.Kind is not JsonDifferenceKind.ArrayElementUnmatched)
        {
            sb.Append('\n').Append(indent).Append("actual:   ").Append(difference.Actual);
        }
    }

    /// <summary>Maps a difference category to the reason shown after its location.</summary>
    private static string DescribeDifference(JsonDifferenceKind kind) => kind switch
    {
        JsonDifferenceKind.Value => "the value differs",
        JsonDifferenceKind.Kind => "the JSON kind differs",
        JsonDifferenceKind.MissingProperty => "the property is missing from the actual document",
        JsonDifferenceKind.UnexpectedProperty => "the property is not in the expected document",
        JsonDifferenceKind.ArrayLength => "the array length differs",
        _ => "no element of the actual array is equivalent", // ArrayElementUnmatched
    };

    /// <summary>Throws unless <paramref name="resolution"/> is a failed resolution that carries its failed segment, as
    /// <see cref="JsonPath.Resolve"/> returns it.</summary>
    /// <param name="resolution">The resolution passed to a public factory.</param>
    /// <param name="factory">The factory's name, for the message.</param>
    private static void RequireFailed(JsonPathResolution resolution, string factory)
    {
        if (resolution.Found || resolution.FailedSegment is null)
        {
            throw new ArgumentException(
                factory + " renders a failed resolution, as JsonPath.Resolve returns it; this one is "
                + (resolution.Found ? "found." : "a default resolution."),
                nameof(resolution));
        }
    }

    /// <summary>Throws when <paramref name="resolution"/> is a <see langword="default"/> resolution, which no
    /// <see cref="JsonPath"/> call returns.</summary>
    /// <param name="resolution">The resolution passed to a public factory.</param>
    /// <param name="factory">The factory's name, for the message.</param>
    private static void RequireResolved(JsonPathResolution resolution, string factory)
    {
        if (!resolution.Found && resolution.FailedSegment is null)
        {
            throw new ArgumentException(
                factory + " renders a resolution from JsonPath.Resolve; this one is a default resolution.",
                nameof(resolution));
        }
    }

    /// <summary>Renders a resolved element compactly: scalars as JSON literals (a long string truncated), objects
    /// and arrays by kind only (their full text would bloat the message).</summary>
    private static string RenderElement(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => Quote(Truncate(element.GetString()!)),
        JsonValueKind.Number => element.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Null => "null",
        _ => DescribeKind(element.ValueKind), // Object, Array, or the Undefined of a default element
    };

    /// <summary>Renders a resolved element for a shape failure: an array reports its length,
    /// any other kind reports the kind alone.</summary>
    private static string RenderShape(JsonElement element)
    {
        if (element.ValueKind is not JsonValueKind.Array)
        {
            return DescribeKind(element.ValueKind);
        }

        var length = element.GetArrayLength();
        return length is 0 ? "an empty array" : "an array of length " + length.ToString(CultureInfo.InvariantCulture);
    }

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

    /// <summary>Appends <c>, which is not a JSON integer literal</c> to <paramref name="found"/> when the resolution found
    /// a JSON string that an integer overload does not read.</summary>
    private static string WithIntegerNote(JsonPathResolution resolution, string found)
        => resolution.Found
           && resolution.Element.ValueKind is JsonValueKind.String
           && !JsonValueComparison.IsJsonIntegerLiteral(resolution.Element.GetString()!)
            ? found + ", which is not a JSON integer literal"
            : found;

    /// <summary>Renders a count with its noun, such as <c>1 element</c> or <c>2 elements</c>.</summary>
    private static string CountOf(int count, string noun)
        => count.ToString(CultureInfo.InvariantCulture) + " " + noun + (count is 1 ? string.Empty : "s");

    /// <summary>The <c>; body: …</c> suffix of a response failure, empty when the body is.</summary>
    private static string BodySuffix(string body)
        => body.Length is 0 ? string.Empty : "; body: " + TruncateBody(body);

    /// <summary>Renders a deserialized value by its <see cref="object.ToString"/>; a <see langword="null"/> value
    /// renders as the word null.</summary>
    private static string RenderObject(object? value)
        => value is null ? "null" : string.Concat(value);

    /// <summary>Caps a response-body string so a large payload cannot bloat the message.</summary>
    private static string TruncateBody(string body)
        => body.Length <= MaxResponseBodyLength
            ? body
            : body[..MaxResponseBodyLength] + "...";

    /// <summary>Caps a rendered string value so a large payload cannot bloat the message.</summary>
    private static string Truncate(string value)
        => value.Length <= MaxRenderedStringLength
            ? value
            : value[..MaxRenderedStringLength] + "...";
}
