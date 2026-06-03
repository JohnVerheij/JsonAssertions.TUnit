using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json;
using JsonAssertions;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace JsonAssertions.TUnit;

/// <summary>
/// Fluent TUnit entry points for asserting the value at a dot-separated JSON path. Each method
/// is generated into a TUnit assertion chain via the <c>[GenerateAssertion]</c> source
/// generator and delegates to <see cref="JsonPath"/> and <see cref="JsonValueComparison"/> in
/// the framework-agnostic core. Both a JSON <see cref="string"/> and a <see cref="JsonElement"/>
/// are accepted as the asserted value; the expected value may be a <see cref="string"/>,
/// a <see cref="bool"/>, or a number. The integer overloads (<see cref="int"/> / <see cref="uint"/>
/// / <see cref="long"/> / <see cref="ulong"/>) each match the value whether the JSON encodes it as a
/// number or as a numeric string, because the encoding depends on the serializer (System.Text.Json
/// writes a number; Protobuf's <c>JsonFormatter</c> can emit a string, and serializes 64-bit ints as
/// strings to avoid 53-bit precision loss); the <see cref="double"/> overload matches a JSON number.
/// </summary>
/// <remarks>
/// The methods return <see cref="AssertionResult"/> so a failure surfaces either where the
/// path resolution stopped, or the value that was found in place of the expected one (via
/// <see cref="JsonFailureMessage"/>). The <see cref="string"/> overloads parse into a
/// disposable <see cref="JsonDocument"/> and delegate to the <see cref="JsonElement"/> overload
/// <em>within</em> the document's lifetime, because a <see cref="JsonElement"/> is only valid
/// while its backing document is alive.
/// </remarks>
[SuppressMessage(
    "Performance",
    "MA0109:Consider adding an overload with a Span<T> or Memory<T>",
    Justification = "The one-of overloads take T[] so callers can use a C# 12 collection expression literal (HasJsonValueOneOf(\"status\", [\"Healthy\", \"Degraded\"])); a ReadOnlySpan<T> overload cannot be expressed under TUnit's [GenerateAssertion] source generator (ref struct parameters are unsupported), and assertion call sites do not need the allocation profile a Span overload would provide.")]
public static class JsonValueAssertions
{
    /// <summary>Asserts the JSON string has the string value <paramref name="expected"/> at
    /// the dot-separated <paramref name="path"/>.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.name</c>.</param>
    /// <param name="expected">The expected string value.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonValue(this string json, string path, string expected)
        => JsonStringSource.Assert(json, root => HasJsonValue(root, path, expected));

    /// <summary>Asserts the JSON element has the string value <paramref name="expected"/> at
    /// the dot-separated <paramref name="path"/>.</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.name</c>.</param>
    /// <param name="expected">The expected string value.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonValue(this JsonElement element, string path, string expected)
    {
        var resolution = JsonPath.Resolve(element, path);
        return resolution.Found && JsonValueComparison.Matches(resolution.Element, expected)
            ? AssertionResult.Passed
            : AssertionResult.Failed(JsonFailureMessage.ValueMismatch(path, resolution, "\"" + expected + "\""));
    }

    /// <summary>Asserts the JSON string has the boolean value <paramref name="expected"/> at
    /// the dot-separated <paramref name="path"/>.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.active</c>.</param>
    /// <param name="expected">The expected boolean value.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonValue(this string json, string path, bool expected)
        => JsonStringSource.Assert(json, root => HasJsonValue(root, path, expected));

    /// <summary>Asserts the JSON element has the boolean value <paramref name="expected"/> at
    /// the dot-separated <paramref name="path"/>.</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.active</c>.</param>
    /// <param name="expected">The expected boolean value.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonValue(this JsonElement element, string path, bool expected)
    {
        var resolution = JsonPath.Resolve(element, path);
        if (resolution.Found && JsonValueComparison.Matches(resolution.Element, expected))
        {
            return AssertionResult.Passed;
        }

        var expectedDescription = expected ? "true" : "false";
        return AssertionResult.Failed(JsonFailureMessage.ValueMismatch(path, resolution, expectedDescription));
    }

    /// <summary>Asserts the JSON string has the numeric value <paramref name="expected"/> at
    /// the dot-separated <paramref name="path"/>.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.age</c>.</param>
    /// <param name="expected">The expected numeric value.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonValue(this string json, string path, double expected)
        => JsonStringSource.Assert(json, root => HasJsonValue(root, path, expected));

    /// <summary>Asserts the JSON element has the numeric value <paramref name="expected"/> at
    /// the dot-separated <paramref name="path"/>.</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.age</c>.</param>
    /// <param name="expected">The expected numeric value.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonValue(this JsonElement element, string path, double expected)
    {
        var resolution = JsonPath.Resolve(element, path);
        return resolution.Found && JsonValueComparison.Matches(resolution.Element, expected)
            ? AssertionResult.Passed
            : AssertionResult.Failed(JsonFailureMessage.ValueMismatch(
                path, resolution, expected.ToString(CultureInfo.InvariantCulture)));
    }

    /// <summary>Asserts the JSON string has the 32-bit integer value <paramref name="expected"/>
    /// at the dot-separated <paramref name="path"/>, matching whether the JSON encodes it as a
    /// number or as a numeric string. System.Text.Json writes <c>int32</c> as a JSON number while
    /// Protobuf's <c>JsonFormatter</c> can emit a JSON string; both are matched.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.age</c>.</param>
    /// <param name="expected">The expected 32-bit integer value.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonValue(this string json, string path, int expected)
        => JsonStringSource.Assert(json, root => HasJsonValue(root, path, expected));

    /// <summary>Asserts the JSON element has the 32-bit integer value <paramref name="expected"/>
    /// at the dot-separated <paramref name="path"/>, matching a JSON number or a numeric JSON
    /// string and comparing exactly.</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.age</c>.</param>
    /// <param name="expected">The expected 32-bit integer value.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonValue(this JsonElement element, string path, int expected)
    {
        var resolution = JsonPath.Resolve(element, path);
        return resolution.Found && JsonValueComparison.Matches(resolution.Element, expected)
            ? AssertionResult.Passed
            : AssertionResult.Failed(JsonFailureMessage.ValueMismatch(
                path, resolution, expected.ToString(CultureInfo.InvariantCulture)));
    }

    /// <summary>Asserts the JSON string has the unsigned 32-bit integer value
    /// <paramref name="expected"/> at the dot-separated <paramref name="path"/>, matching whether the
    /// JSON encodes it as a number or as a numeric string. System.Text.Json writes <c>uint32</c> as a
    /// JSON number while Protobuf's <c>JsonFormatter</c> can emit a JSON string; both are
    /// matched.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.age</c>.</param>
    /// <param name="expected">The expected unsigned 32-bit integer value.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonValue(this string json, string path, uint expected)
        => JsonStringSource.Assert(json, root => HasJsonValue(root, path, expected));

    /// <summary>Asserts the JSON element has the unsigned 32-bit integer value
    /// <paramref name="expected"/> at the dot-separated <paramref name="path"/>, matching a JSON
    /// number or a numeric JSON string and comparing exactly.</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.age</c>.</param>
    /// <param name="expected">The expected unsigned 32-bit integer value.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonValue(this JsonElement element, string path, uint expected)
    {
        var resolution = JsonPath.Resolve(element, path);
        return resolution.Found && JsonValueComparison.Matches(resolution.Element, expected)
            ? AssertionResult.Passed
            : AssertionResult.Failed(JsonFailureMessage.ValueMismatch(
                path, resolution, expected.ToString(CultureInfo.InvariantCulture)));
    }

    /// <summary>Asserts the JSON string has the 64-bit integer value <paramref name="expected"/>
    /// at the dot-separated <paramref name="path"/>, matching whether the JSON encodes it as a
    /// number or as a numeric string (parsed with <see cref="CultureInfo.InvariantCulture"/>).
    /// Protobuf's <c>JsonFormatter</c> serializes <c>int64</c> as a JSON string to avoid the 53-bit
    /// precision loss a JSON number would incur, while System.Text.Json writes it as a JSON number;
    /// both are matched.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A dot-separated property path, for example <c>guid.high</c>.</param>
    /// <param name="expected">The expected 64-bit integer value.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonValue(this string json, string path, long expected)
        => JsonStringSource.Assert(json, root => HasJsonValue(root, path, expected));

    /// <summary>Asserts the JSON element has the 64-bit integer value <paramref name="expected"/>
    /// at the dot-separated <paramref name="path"/>, matching a JSON number or a numeric JSON string
    /// (parsed with <see cref="CultureInfo.InvariantCulture"/>) and comparing exactly.</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A dot-separated property path, for example <c>guid.high</c>.</param>
    /// <param name="expected">The expected 64-bit integer value.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonValue(this JsonElement element, string path, long expected)
    {
        var resolution = JsonPath.Resolve(element, path);
        return resolution.Found && JsonValueComparison.Matches(resolution.Element, expected)
            ? AssertionResult.Passed
            : AssertionResult.Failed(JsonFailureMessage.ValueMismatch(
                path, resolution, expected.ToString(CultureInfo.InvariantCulture)));
    }

    /// <summary>Asserts the JSON string has the unsigned 64-bit integer value
    /// <paramref name="expected"/> at the dot-separated <paramref name="path"/>, matching whether the
    /// JSON encodes it as a number or as a numeric string (parsed with
    /// <see cref="CultureInfo.InvariantCulture"/>). Protobuf's <c>JsonFormatter</c> serializes
    /// <c>uint64</c> as a JSON string for the same precision reason as <c>int64</c>, while
    /// System.Text.Json writes it as a JSON number; both are matched.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A dot-separated property path, for example <c>guid.low</c>.</param>
    /// <param name="expected">The expected unsigned 64-bit integer value.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonValue(this string json, string path, ulong expected)
        => JsonStringSource.Assert(json, root => HasJsonValue(root, path, expected));

    /// <summary>Asserts the JSON element has the unsigned 64-bit integer value
    /// <paramref name="expected"/> at the dot-separated <paramref name="path"/>, matching a JSON
    /// number or a numeric JSON string (parsed with <see cref="CultureInfo.InvariantCulture"/>) and
    /// comparing exactly.</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A dot-separated property path, for example <c>guid.low</c>.</param>
    /// <param name="expected">The expected unsigned 64-bit integer value.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonValue(this JsonElement element, string path, ulong expected)
    {
        var resolution = JsonPath.Resolve(element, path);
        return resolution.Found && JsonValueComparison.Matches(resolution.Element, expected)
            ? AssertionResult.Passed
            : AssertionResult.Failed(JsonFailureMessage.ValueMismatch(
                path, resolution, expected.ToString(CultureInfo.InvariantCulture)));
    }

    /// <summary>Asserts the value at <paramref name="path"/> satisfies the
    /// <paramref name="predicate"/>. When <paramref name="path"/> contains the <c>[*]</c> wildcard,
    /// the predicate must hold on every expanded element; the failure names the first element that is
    /// missing or fails, and an empty match set passes vacuously. The predicate sees the resolved
    /// <see cref="JsonElement"/> and decides pass/fail; failure messages do not surface the
    /// predicate's intent (use a descriptive call site or wrap the assertion in
    /// <c>.Because(...)</c>).</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>user.age</c>, optionally with the <c>[*]</c> wildcard to match every
    /// element of an array (for example <c>[*].on</c>).</param>
    /// <param name="predicate">A predicate that returns <see langword="true"/> for matching
    /// elements; receives each element resolved at <paramref name="path"/>.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonValueMatching(this string json, string path, Func<JsonElement, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return JsonStringSource.Assert(json, root => HasJsonValueMatching(root, path, predicate));
    }

    /// <summary>Asserts the value at <paramref name="path"/> satisfies
    /// <paramref name="predicate"/>. When <paramref name="path"/> contains the <c>[*]</c> wildcard,
    /// the predicate must hold on every expanded element; the failure names the first element that is
    /// missing or fails, and an empty match set passes vacuously.</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>user.age</c>, optionally with the <c>[*]</c> wildcard to match every
    /// element of an array.</param>
    /// <param name="predicate">A predicate that returns <see langword="true"/> for matching
    /// elements.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonValueMatching(this JsonElement element, string path, Func<JsonElement, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        if (JsonPath.ContainsWildcard(path))
        {
            // A `[*]` path requires the predicate to hold on every expanded element; report the
            // first that is missing or fails the predicate. An empty array passes vacuously.
            foreach (var expanded in JsonPath.ResolveAll(element, path))
            {
                if (!expanded.Found || !predicate(expanded.Element))
                {
                    // Report the concrete expanded path (e.g. "[1].on"), not the wildcard, so the
                    // failure names which element failed, consistent with HasJsonProperty.
                    return AssertionResult.Failed(JsonFailureMessage.ValueMismatch(expanded.ResolvedPrefix, expanded, "a value matching the predicate"));
                }
            }

            return AssertionResult.Passed;
        }

        var resolution = JsonPath.Resolve(element, path);
        return resolution.Found && predicate(resolution.Element)
            ? AssertionResult.Passed
            : AssertionResult.Failed(JsonFailureMessage.ValueMismatch(path, resolution, "a value matching the predicate"));
    }

    /// <summary>Asserts the value at <paramref name="path"/> is a JSON string equal to any of
    /// <paramref name="candidates"/>. The discoverable form of "value is one of {a, b, c}".</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>health.status</c>.</param>
    /// <param name="candidates">The acceptable string values; ordinal comparison.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonValueOneOf(this string json, string path, string[] candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        return JsonStringSource.Assert(json, root => HasJsonValueOneOf(root, path, candidates));
    }

    /// <summary>Asserts the value at <paramref name="path"/> is a JSON string equal to any of
    /// <paramref name="candidates"/>.</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>health.status</c>.</param>
    /// <param name="candidates">The acceptable string values; ordinal comparison.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonValueOneOf(this JsonElement element, string path, string[] candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        var resolution = JsonPath.Resolve(element, path);
        return resolution.Found && JsonValueComparison.MatchesAny(resolution.Element, candidates)
            ? AssertionResult.Passed
            : AssertionResult.Failed(JsonFailureMessage.ValueMismatch(path, resolution, FormatOneOf(candidates)));
    }

    /// <summary>Asserts the value at <paramref name="path"/> is a JSON number equal to any of
    /// <paramref name="candidates"/>. Values beyond <see cref="double"/> precision are out of
    /// scope for this overload.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>response.code</c>.</param>
    /// <param name="candidates">The acceptable numeric values.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonValueOneOf(this string json, string path, double[] candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        return JsonStringSource.Assert(json, root => HasJsonValueOneOf(root, path, candidates));
    }

    /// <summary>Asserts the value at <paramref name="path"/> is a JSON number equal to any of
    /// <paramref name="candidates"/>.</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>response.code</c>.</param>
    /// <param name="candidates">The acceptable numeric values.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonValueOneOf(this JsonElement element, string path, double[] candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        var resolution = JsonPath.Resolve(element, path);
        return resolution.Found && JsonValueComparison.MatchesAny(resolution.Element, candidates)
            ? AssertionResult.Passed
            : AssertionResult.Failed(JsonFailureMessage.ValueMismatch(path, resolution, FormatOneOf(candidates)));
    }

    /// <summary>Asserts the value at <paramref name="path"/> is a 32-bit integer equal to any of
    /// <paramref name="candidates"/>, encoded as either a JSON number or a numeric JSON string, for
    /// System.Text.Json or Protobuf-style <c>int32</c> values.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>response.code</c>.</param>
    /// <param name="candidates">The acceptable 32-bit integer values.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonValueOneOf(this string json, string path, int[] candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        return JsonStringSource.Assert(json, root => HasJsonValueOneOf(root, path, candidates));
    }

    /// <summary>Asserts the value at <paramref name="path"/> is a 32-bit integer equal to any of
    /// <paramref name="candidates"/>, encoded as either a JSON number or a numeric JSON
    /// string.</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>response.code</c>.</param>
    /// <param name="candidates">The acceptable 32-bit integer values.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonValueOneOf(this JsonElement element, string path, int[] candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        var resolution = JsonPath.Resolve(element, path);
        return resolution.Found && JsonValueComparison.MatchesAny(resolution.Element, candidates)
            ? AssertionResult.Passed
            : AssertionResult.Failed(JsonFailureMessage.ValueMismatch(path, resolution, FormatOneOf(candidates)));
    }

    /// <summary>Asserts the value at <paramref name="path"/> is an unsigned 32-bit integer equal to
    /// any of <paramref name="candidates"/>, encoded as either a JSON number or a numeric JSON
    /// string, for System.Text.Json or Protobuf-style <c>uint32</c> values.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>response.code</c>.</param>
    /// <param name="candidates">The acceptable unsigned 32-bit integer values.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonValueOneOf(this string json, string path, uint[] candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        return JsonStringSource.Assert(json, root => HasJsonValueOneOf(root, path, candidates));
    }

    /// <summary>Asserts the value at <paramref name="path"/> is an unsigned 32-bit integer equal to
    /// any of <paramref name="candidates"/>, encoded as either a JSON number or a numeric JSON
    /// string.</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>response.code</c>.</param>
    /// <param name="candidates">The acceptable unsigned 32-bit integer values.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonValueOneOf(this JsonElement element, string path, uint[] candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        var resolution = JsonPath.Resolve(element, path);
        return resolution.Found && JsonValueComparison.MatchesAny(resolution.Element, candidates)
            ? AssertionResult.Passed
            : AssertionResult.Failed(JsonFailureMessage.ValueMismatch(path, resolution, FormatOneOf(candidates)));
    }

    /// <summary>Asserts the value at <paramref name="path"/> is a 64-bit integer equal to any of
    /// <paramref name="candidates"/>, encoded as either a JSON number or a numeric JSON string
    /// (parsed with <see cref="CultureInfo.InvariantCulture"/>), for System.Text.Json or
    /// Protobuf-style <c>int64</c> values.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>message.sequence</c>.</param>
    /// <param name="candidates">The acceptable 64-bit integer values.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonValueOneOf(this string json, string path, long[] candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        return JsonStringSource.Assert(json, root => HasJsonValueOneOf(root, path, candidates));
    }

    /// <summary>Asserts the value at <paramref name="path"/> is a 64-bit integer equal to any of
    /// <paramref name="candidates"/>, encoded as either a JSON number or a numeric JSON string
    /// (parsed with <see cref="CultureInfo.InvariantCulture"/>).</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>message.sequence</c>.</param>
    /// <param name="candidates">The acceptable 64-bit integer values.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonValueOneOf(this JsonElement element, string path, long[] candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        var resolution = JsonPath.Resolve(element, path);
        return resolution.Found && JsonValueComparison.MatchesAny(resolution.Element, candidates)
            ? AssertionResult.Passed
            : AssertionResult.Failed(JsonFailureMessage.ValueMismatch(path, resolution, FormatOneOf(candidates)));
    }

    /// <summary>Asserts the value at <paramref name="path"/> is an unsigned 64-bit integer equal to
    /// any of <paramref name="candidates"/>, encoded as either a JSON number or a numeric JSON string
    /// (parsed with <see cref="CultureInfo.InvariantCulture"/>), for System.Text.Json or
    /// Protobuf-style <c>uint64</c> values.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>message.sequence</c>.</param>
    /// <param name="candidates">The acceptable unsigned 64-bit integer values.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonValueOneOf(this string json, string path, ulong[] candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        return JsonStringSource.Assert(json, root => HasJsonValueOneOf(root, path, candidates));
    }

    /// <summary>Asserts the value at <paramref name="path"/> is an unsigned 64-bit integer equal to
    /// any of <paramref name="candidates"/>, encoded as either a JSON number or a numeric JSON string
    /// (parsed with <see cref="CultureInfo.InvariantCulture"/>).</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>message.sequence</c>.</param>
    /// <param name="candidates">The acceptable unsigned 64-bit integer values.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonValueOneOf(this JsonElement element, string path, ulong[] candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        var resolution = JsonPath.Resolve(element, path);
        return resolution.Found && JsonValueComparison.MatchesAny(resolution.Element, candidates)
            ? AssertionResult.Passed
            : AssertionResult.Failed(JsonFailureMessage.ValueMismatch(path, resolution, FormatOneOf(candidates)));
    }

    /// <summary>Asserts the value at <paramref name="path"/> is a JSON string whose text
    /// parses as <typeparamref name="T"/> via <see cref="IParsable{T}.TryParse(string, IFormatProvider, out T)"/>
    /// against <see cref="CultureInfo.InvariantCulture"/>. Covers the "value parses as
    /// <see cref="Guid"/> / <see cref="DateTimeOffset"/> / <see cref="Uri"/>" pattern without
    /// committing to a particular parser per call site.</summary>
    /// <typeparam name="T">The target type implementing <see cref="IParsable{T}"/>.</typeparam>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>order.id</c>.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonValueParsableAs<T>(this string json, string path)
        where T : IParsable<T>
        => JsonStringSource.Assert(json, root => HasJsonValueParsableAs<T>(root, path));

    /// <summary>Asserts the value at <paramref name="path"/> is a JSON string whose text
    /// parses as <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The target type implementing <see cref="IParsable{T}"/>.</typeparam>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>order.id</c>.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonValueParsableAs<T>(this JsonElement element, string path)
        where T : IParsable<T>
    {
        var resolution = JsonPath.Resolve(element, path);
        var description = "a JSON string parseable as " + typeof(T).Name;
        if (!resolution.Found || resolution.Element.ValueKind is not JsonValueKind.String)
        {
            return AssertionResult.Failed(JsonFailureMessage.ValueMismatch(path, resolution, description));
        }

        var raw = resolution.Element.GetString();
        return raw is not null && T.TryParse(raw, CultureInfo.InvariantCulture, out _)
            ? AssertionResult.Passed
            : AssertionResult.Failed(JsonFailureMessage.ValueMismatch(path, resolution, description));
    }

    /// <summary>Formats a one-of candidate list for failure messages: <c>{ "a", "b" }</c>
    /// for strings and <c>{ 1, 2 }</c> for numerics. Strings are JSON-escaped so a candidate
    /// containing a quote, backslash, or control character renders unambiguously.</summary>
    private static string FormatOneOf(string[] candidates)
    {
        var parts = new string[candidates.Length];
        for (var i = 0; i < candidates.Length; i++)
        {
            parts[i] = "\"" + EscapeForFailureMessage(candidates[i]) + "\"";
        }

        return "one of { " + string.Join(", ", parts) + " }";
    }

    /// <summary>Formats a one-of candidate list of numerics.</summary>
    private static string FormatOneOf(double[] candidates)
    {
        var parts = new string[candidates.Length];
        for (var i = 0; i < candidates.Length; i++)
        {
            parts[i] = candidates[i].ToString(CultureInfo.InvariantCulture);
        }

        return "one of { " + string.Join(", ", parts) + " }";
    }

    /// <summary>Formats a one-of candidate list of 32-bit integers. The candidates are unquoted:
    /// they denote the integer value, matched whether the JSON encodes it as a number or a numeric
    /// string.</summary>
    private static string FormatOneOf(int[] candidates)
    {
        var parts = new string[candidates.Length];
        for (var i = 0; i < candidates.Length; i++)
        {
            parts[i] = candidates[i].ToString(CultureInfo.InvariantCulture);
        }

        return "one of { " + string.Join(", ", parts) + " }";
    }

    /// <summary>Formats a one-of candidate list of unsigned 32-bit integers. The candidates are
    /// unquoted: they denote the integer value, matched whether the JSON encodes it as a number or a
    /// numeric string.</summary>
    private static string FormatOneOf(uint[] candidates)
    {
        var parts = new string[candidates.Length];
        for (var i = 0; i < candidates.Length; i++)
        {
            parts[i] = candidates[i].ToString(CultureInfo.InvariantCulture);
        }

        return "one of { " + string.Join(", ", parts) + " }";
    }

    /// <summary>Formats a one-of candidate list of 64-bit integers. The candidates are unquoted:
    /// they denote the integer value, matched whether the JSON encodes it as a number or a numeric
    /// string.</summary>
    private static string FormatOneOf(long[] candidates)
    {
        var parts = new string[candidates.Length];
        for (var i = 0; i < candidates.Length; i++)
        {
            parts[i] = candidates[i].ToString(CultureInfo.InvariantCulture);
        }

        return "one of { " + string.Join(", ", parts) + " }";
    }

    /// <summary>Formats a one-of candidate list of unsigned 64-bit integers. The candidates are
    /// unquoted: they denote the integer value, matched whether the JSON encodes it as a number or a
    /// numeric string.</summary>
    private static string FormatOneOf(ulong[] candidates)
    {
        var parts = new string[candidates.Length];
        for (var i = 0; i < candidates.Length; i++)
        {
            parts[i] = candidates[i].ToString(CultureInfo.InvariantCulture);
        }

        return "one of { " + string.Join(", ", parts) + " }";
    }

    /// <summary>Escapes JSON-style structural characters (<c>"</c>, <c>\</c>) and ASCII
    /// control characters in <paramref name="value"/> so a candidate containing them renders
    /// unambiguously inside the quoted form of a failure message. AOT-safe (no reflection,
    /// no <see cref="JsonSerializer"/> dependency).</summary>
    private static string EscapeForFailureMessage(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            switch (ch)
            {
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '"':
                    sb.Append("\\\"");
                    break;
                case '\b':
                    sb.Append("\\b");
                    break;
                case '\f':
                    sb.Append("\\f");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                default:
                    if (ch < ' ')
                    {
                        // char-to-int via the implicit char->ushort->int widening avoids the
                        // explicit-cast lint while preserving the numeric value for the
                        // \u%04x escape format used by JSON.
                        var codePoint = Convert.ToInt32(ch);
                        sb.Append("\\u").Append(codePoint.ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        sb.Append(ch);
                    }

                    break;
            }
        }

        return sb.ToString();
    }
}
