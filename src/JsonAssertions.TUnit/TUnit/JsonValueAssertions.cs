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
/// number or as a JSON integer literal in a string, because the encoding depends on the serializer (System.Text.Json
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
    private const string NonFiniteExpectation =
        "JSON has no NaN or infinity, so no JSON number can match a non-finite value. Use a finite value; for a "
        + "serializer that writes \"NaN\" or \"Infinity\" as a JSON string, use the string overload.";

    /// <summary>Asserts the JSON string has the string value <paramref name="expected"/> at
    /// the dot-separated <paramref name="path"/>.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.name</c>.</param>
    /// <param name="expected">The expected string value.</param>
    [GenerateAssertion(ExpectationMessage = Expectations.HasJsonString)]
    public static AssertionResult HasJsonValue(this string json, string path, string expected)
        => JsonStringSource.Assert(json, root => HasJsonValue(root, path, expected));

    /// <summary>Asserts the JSON element has the string value <paramref name="expected"/> at
    /// the dot-separated <paramref name="path"/>.</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.name</c>.</param>
    /// <param name="expected">The expected string value.</param>
    [GenerateAssertion(ExpectationMessage = Expectations.HasJsonString)]
    public static AssertionResult HasJsonValue(this JsonElement element, string path, string expected)
    {
        return JsonElementSource.Assert(element, root =>
        {
            var resolution = JsonPath.Resolve(root, path);
            return resolution.Found && JsonValueComparison.Matches(resolution.Element, expected)
                ? AssertionResult.Passed
                : AssertionResult.Failed(JsonFailureMessage.Found(resolution));
        });
    }

    /// <summary>Asserts the JSON string has the boolean value <paramref name="expected"/> at
    /// the dot-separated <paramref name="path"/>.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.active</c>.</param>
    /// <param name="expected">The expected boolean value.</param>
    [GenerateAssertion(ExpectationMessage = Expectations.HasJsonBooleanValue)]
    public static AssertionResult HasJsonValue(this string json, string path, bool expected)
        => JsonStringSource.Assert(json, root => HasJsonValue(root, path, expected));

    /// <summary>Asserts the JSON element has the boolean value <paramref name="expected"/> at
    /// the dot-separated <paramref name="path"/>.</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.active</c>.</param>
    /// <param name="expected">The expected boolean value.</param>
    [GenerateAssertion(ExpectationMessage = Expectations.HasJsonBooleanValue)]
    public static AssertionResult HasJsonValue(this JsonElement element, string path, bool expected)
    {
        return JsonElementSource.Assert(element, root =>
        {
            var resolution = JsonPath.Resolve(root, path);
            if (resolution.Found && JsonValueComparison.Matches(resolution.Element, expected))
            {
                return AssertionResult.Passed;
            }

            return AssertionResult.Failed(JsonFailureMessage.Found(resolution, expected ? "expected true" : "expected false"));
        });
    }

    /// <summary>Asserts the JSON string has the numeric value <paramref name="expected"/> at
    /// the dot-separated <paramref name="path"/>.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.age</c>.</param>
    /// <param name="expected">The expected numeric value.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="expected"/> is NaN or an infinity, which JSON
    /// cannot hold.</exception>
    [GenerateAssertion(ExpectationMessage = Expectations.HasJsonNumber)]
    public static AssertionResult HasJsonValue(this string json, string path, double expected)
    {
        RequireFinite(expected);
        return JsonStringSource.Assert(json, root => HasJsonValue(root, path, expected));
    }

    /// <summary>Asserts the JSON element has the numeric value <paramref name="expected"/> at
    /// the dot-separated <paramref name="path"/>.</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.age</c>.</param>
    /// <param name="expected">The expected numeric value.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="expected"/> is NaN or an infinity, which JSON
    /// cannot hold.</exception>
    [GenerateAssertion(ExpectationMessage = Expectations.HasJsonNumber)]
    public static AssertionResult HasJsonValue(this JsonElement element, string path, double expected)
    {
        RequireFinite(expected);
        return JsonElementSource.Assert(element, root =>
        {
            var resolution = JsonPath.Resolve(root, path);
            return resolution.Found && JsonValueComparison.Matches(resolution.Element, expected)
                ? AssertionResult.Passed
                : AssertionResult.Failed(JsonFailureMessage.Found(
                    resolution, "expected " + expected.ToString(CultureInfo.InvariantCulture)));
        });
    }

    /// <summary>Asserts the JSON string has the 32-bit integer value <paramref name="expected"/>
    /// at the dot-separated <paramref name="path"/>, matching whether the JSON encodes it as a
    /// number or as a JSON integer literal in a string. System.Text.Json writes <c>int32</c> as a JSON number while
    /// Protobuf's <c>JsonFormatter</c> can emit a JSON string; both are matched.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.age</c>.</param>
    /// <param name="expected">The expected 32-bit integer value.</param>
    [GenerateAssertion(ExpectationMessage = Expectations.HasJsonInteger)]
    public static AssertionResult HasJsonValue(this string json, string path, int expected)
        => JsonStringSource.Assert(json, root => HasJsonValue(root, path, expected));

    /// <summary>Asserts the JSON element has the 32-bit integer value <paramref name="expected"/>
    /// at the dot-separated <paramref name="path"/>, matching a JSON number or a JSON integer literal in a
    /// string and comparing exactly.</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.age</c>.</param>
    /// <param name="expected">The expected 32-bit integer value.</param>
    [GenerateAssertion(ExpectationMessage = Expectations.HasJsonInteger)]
    public static AssertionResult HasJsonValue(this JsonElement element, string path, int expected)
    {
        return JsonElementSource.Assert(element, root =>
        {
            var resolution = JsonPath.Resolve(root, path);
            return resolution.Found && JsonValueComparison.Matches(resolution.Element, expected)
                ? AssertionResult.Passed
                : AssertionResult.Failed(JsonFailureMessage.FoundInteger(resolution));
        });
    }

    /// <summary>Asserts the JSON string has the unsigned 32-bit integer value
    /// <paramref name="expected"/> at the dot-separated <paramref name="path"/>, matching whether the
    /// JSON encodes it as a number or as a JSON integer literal in a string. System.Text.Json writes <c>uint32</c> as a
    /// JSON number while Protobuf's <c>JsonFormatter</c> can emit a JSON string; both are
    /// matched.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.age</c>.</param>
    /// <param name="expected">The expected unsigned 32-bit integer value.</param>
    [GenerateAssertion(ExpectationMessage = Expectations.HasJsonInteger)]
    public static AssertionResult HasJsonValue(this string json, string path, uint expected)
        => JsonStringSource.Assert(json, root => HasJsonValue(root, path, expected));

    /// <summary>Asserts the JSON element has the unsigned 32-bit integer value
    /// <paramref name="expected"/> at the dot-separated <paramref name="path"/>, matching a JSON
    /// number or a JSON integer literal in a string and comparing exactly.</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.age</c>.</param>
    /// <param name="expected">The expected unsigned 32-bit integer value.</param>
    [GenerateAssertion(ExpectationMessage = Expectations.HasJsonInteger)]
    public static AssertionResult HasJsonValue(this JsonElement element, string path, uint expected)
    {
        return JsonElementSource.Assert(element, root =>
        {
            var resolution = JsonPath.Resolve(root, path);
            return resolution.Found && JsonValueComparison.Matches(resolution.Element, expected)
                ? AssertionResult.Passed
                : AssertionResult.Failed(JsonFailureMessage.FoundInteger(resolution));
        });
    }

    /// <summary>Asserts the JSON string has the 64-bit integer value <paramref name="expected"/>
    /// at the dot-separated <paramref name="path"/>, matching whether the JSON encodes it as a
    /// number or as a JSON integer literal in a string (parsed with <see cref="CultureInfo.InvariantCulture"/>).
    /// Protobuf's <c>JsonFormatter</c> serializes <c>int64</c> as a JSON string to avoid the 53-bit
    /// precision loss a JSON number would incur, while System.Text.Json writes it as a JSON number;
    /// both are matched.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A dot-separated property path, for example <c>guid.high</c>.</param>
    /// <param name="expected">The expected 64-bit integer value.</param>
    [GenerateAssertion(ExpectationMessage = Expectations.HasJsonInteger)]
    public static AssertionResult HasJsonValue(this string json, string path, long expected)
        => JsonStringSource.Assert(json, root => HasJsonValue(root, path, expected));

    /// <summary>Asserts the JSON element has the 64-bit integer value <paramref name="expected"/>
    /// at the dot-separated <paramref name="path"/>, matching a JSON number or a JSON integer literal in a string
    /// (parsed with <see cref="CultureInfo.InvariantCulture"/>) and comparing exactly.</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A dot-separated property path, for example <c>guid.high</c>.</param>
    /// <param name="expected">The expected 64-bit integer value.</param>
    [GenerateAssertion(ExpectationMessage = Expectations.HasJsonInteger)]
    public static AssertionResult HasJsonValue(this JsonElement element, string path, long expected)
    {
        return JsonElementSource.Assert(element, root =>
        {
            var resolution = JsonPath.Resolve(root, path);
            return resolution.Found && JsonValueComparison.Matches(resolution.Element, expected)
                ? AssertionResult.Passed
                : AssertionResult.Failed(JsonFailureMessage.FoundInteger(resolution));
        });
    }

    /// <summary>Asserts the JSON string has the unsigned 64-bit integer value
    /// <paramref name="expected"/> at the dot-separated <paramref name="path"/>, matching whether the
    /// JSON encodes it as a number or as a JSON integer literal in a string (parsed with
    /// <see cref="CultureInfo.InvariantCulture"/>). Protobuf's <c>JsonFormatter</c> serializes
    /// <c>uint64</c> as a JSON string for the same precision reason as <c>int64</c>, while
    /// System.Text.Json writes it as a JSON number; both are matched.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A dot-separated property path, for example <c>guid.low</c>.</param>
    /// <param name="expected">The expected unsigned 64-bit integer value.</param>
    [GenerateAssertion(ExpectationMessage = Expectations.HasJsonInteger)]
    public static AssertionResult HasJsonValue(this string json, string path, ulong expected)
        => JsonStringSource.Assert(json, root => HasJsonValue(root, path, expected));

    /// <summary>Asserts the JSON element has the unsigned 64-bit integer value
    /// <paramref name="expected"/> at the dot-separated <paramref name="path"/>, matching a JSON
    /// number or a JSON integer literal in a string (parsed with <see cref="CultureInfo.InvariantCulture"/>) and
    /// comparing exactly.</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A dot-separated property path, for example <c>guid.low</c>.</param>
    /// <param name="expected">The expected unsigned 64-bit integer value.</param>
    [GenerateAssertion(ExpectationMessage = Expectations.HasJsonInteger)]
    public static AssertionResult HasJsonValue(this JsonElement element, string path, ulong expected)
    {
        return JsonElementSource.Assert(element, root =>
        {
            var resolution = JsonPath.Resolve(root, path);
            return resolution.Found && JsonValueComparison.Matches(resolution.Element, expected)
                ? AssertionResult.Passed
                : AssertionResult.Failed(JsonFailureMessage.FoundInteger(resolution));
        });
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
    [GenerateAssertion(ExpectationMessage = Expectations.HasJsonValueMatching)]
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
    [GenerateAssertion(ExpectationMessage = Expectations.HasJsonValueMatching)]
    public static AssertionResult HasJsonValueMatching(this JsonElement element, string path, Func<JsonElement, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return JsonElementSource.Assert(element, root =>
        {
            if (JsonPath.ContainsWildcard(path))
            {
                // A `[*]` path requires the predicate to hold on every expanded element; report the
                // first that is missing or fails the predicate. An empty array passes vacuously.
                foreach (var expanded in JsonPath.ResolveAll(root, path))
                {
                    // Name the concrete element (e.g. "[1].on"), not the wildcard.
                    if (!expanded.Found)
                    {
                        return AssertionResult.Failed(
                            "'" + JsonFailureMessage.FailedLocation(expanded) + "' did not resolve: " + JsonFailureMessage.DescribeFailure(expanded));
                    }

                    if (!predicate(expanded.Element))
                    {
                        return AssertionResult.Failed(JsonFailureMessage.Found(expanded) + " at '" + JsonFailureMessage.Name(expanded.ResolvedPrefix) + "'");
                    }
                }

                return AssertionResult.Passed;
            }

            var resolution = JsonPath.Resolve(root, path);
            return resolution.Found && predicate(resolution.Element)
                ? AssertionResult.Passed
                : AssertionResult.Failed(JsonFailureMessage.Found(resolution));
        });
    }

    /// <summary>Asserts the value at <paramref name="path"/> is a JSON string equal to any of
    /// <paramref name="candidates"/>. The discoverable form of "value is one of {a, b, c}".</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>health.status</c>.</param>
    /// <param name="candidates">The acceptable string values; ordinal comparison.</param>
    [GenerateAssertion(ExpectationMessage = Expectations.HasJsonValueOneOf)]
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
    [GenerateAssertion(ExpectationMessage = Expectations.HasJsonValueOneOf)]
    public static AssertionResult HasJsonValueOneOf(this JsonElement element, string path, string[] candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        return JsonElementSource.Assert(element, root =>
        {
            var resolution = JsonPath.Resolve(root, path);
            return resolution.Found && JsonValueComparison.MatchesAny(resolution.Element, candidates)
                ? AssertionResult.Passed
                : AssertionResult.Failed(JsonFailureMessage.Found(resolution, "candidates: " + FormatCandidates(candidates)));
        });
    }

    /// <summary>Asserts the value at <paramref name="path"/> is a JSON number equal to any of
    /// <paramref name="candidates"/>. Values beyond <see cref="double"/> precision are out of
    /// scope for this overload.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>response.code</c>.</param>
    /// <param name="candidates">The acceptable numeric values.</param>
    /// <exception cref="ArgumentOutOfRangeException">A candidate is NaN or an infinity, which JSON cannot
    /// hold.</exception>
    [GenerateAssertion(ExpectationMessage = Expectations.HasJsonValueOneOf)]
    public static AssertionResult HasJsonValueOneOf(this string json, string path, double[] candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        RequireFinite(candidates);
        return JsonStringSource.Assert(json, root => HasJsonValueOneOf(root, path, candidates));
    }

    /// <summary>Asserts the value at <paramref name="path"/> is a JSON number equal to any of
    /// <paramref name="candidates"/>.</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>response.code</c>.</param>
    /// <param name="candidates">The acceptable numeric values.</param>
    /// <exception cref="ArgumentOutOfRangeException">A candidate is NaN or an infinity, which JSON cannot
    /// hold.</exception>
    [GenerateAssertion(ExpectationMessage = Expectations.HasJsonValueOneOf)]
    public static AssertionResult HasJsonValueOneOf(this JsonElement element, string path, double[] candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        RequireFinite(candidates);

        return JsonElementSource.Assert(element, root =>
        {
            var resolution = JsonPath.Resolve(root, path);
            return resolution.Found && JsonValueComparison.MatchesAny(resolution.Element, candidates)
                ? AssertionResult.Passed
                : AssertionResult.Failed(JsonFailureMessage.Found(resolution, "candidates: " + FormatCandidates(candidates)));
        });
    }

    /// <summary>Asserts the value at <paramref name="path"/> is a 32-bit integer equal to any of
    /// <paramref name="candidates"/>, encoded as either a JSON number or a JSON integer literal in a string, for
    /// System.Text.Json or Protobuf-style <c>int32</c> values.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>response.code</c>.</param>
    /// <param name="candidates">The acceptable 32-bit integer values.</param>
    [GenerateAssertion(ExpectationMessage = Expectations.HasJsonValueOneOf)]
    public static AssertionResult HasJsonValueOneOf(this string json, string path, int[] candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        return JsonStringSource.Assert(json, root => HasJsonValueOneOf(root, path, candidates));
    }

    /// <summary>Asserts the value at <paramref name="path"/> is a 32-bit integer equal to any of
    /// <paramref name="candidates"/>, encoded as either a JSON number or a JSON integer literal in a
    /// string.</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>response.code</c>.</param>
    /// <param name="candidates">The acceptable 32-bit integer values.</param>
    [GenerateAssertion(ExpectationMessage = Expectations.HasJsonValueOneOf)]
    public static AssertionResult HasJsonValueOneOf(this JsonElement element, string path, int[] candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        return JsonElementSource.Assert(element, root =>
        {
            var resolution = JsonPath.Resolve(root, path);
            return resolution.Found && JsonValueComparison.MatchesAny(resolution.Element, candidates)
                ? AssertionResult.Passed
                : AssertionResult.Failed(JsonFailureMessage.FoundInteger(resolution, "candidates: " + FormatCandidates(candidates)));
        });
    }

    /// <summary>Asserts the value at <paramref name="path"/> is an unsigned 32-bit integer equal to
    /// any of <paramref name="candidates"/>, encoded as either a JSON number or a JSON integer literal in a
    /// string, for System.Text.Json or Protobuf-style <c>uint32</c> values.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>response.code</c>.</param>
    /// <param name="candidates">The acceptable unsigned 32-bit integer values.</param>
    [GenerateAssertion(ExpectationMessage = Expectations.HasJsonValueOneOf)]
    public static AssertionResult HasJsonValueOneOf(this string json, string path, uint[] candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        return JsonStringSource.Assert(json, root => HasJsonValueOneOf(root, path, candidates));
    }

    /// <summary>Asserts the value at <paramref name="path"/> is an unsigned 32-bit integer equal to
    /// any of <paramref name="candidates"/>, encoded as either a JSON number or a JSON integer literal in a
    /// string.</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>response.code</c>.</param>
    /// <param name="candidates">The acceptable unsigned 32-bit integer values.</param>
    [GenerateAssertion(ExpectationMessage = Expectations.HasJsonValueOneOf)]
    public static AssertionResult HasJsonValueOneOf(this JsonElement element, string path, uint[] candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        return JsonElementSource.Assert(element, root =>
        {
            var resolution = JsonPath.Resolve(root, path);
            return resolution.Found && JsonValueComparison.MatchesAny(resolution.Element, candidates)
                ? AssertionResult.Passed
                : AssertionResult.Failed(JsonFailureMessage.FoundInteger(resolution, "candidates: " + FormatCandidates(candidates)));
        });
    }

    /// <summary>Asserts the value at <paramref name="path"/> is a 64-bit integer equal to any of
    /// <paramref name="candidates"/>, encoded as either a JSON number or a JSON integer literal in a string
    /// (parsed with <see cref="CultureInfo.InvariantCulture"/>), for System.Text.Json or
    /// Protobuf-style <c>int64</c> values.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>message.sequence</c>.</param>
    /// <param name="candidates">The acceptable 64-bit integer values.</param>
    [GenerateAssertion(ExpectationMessage = Expectations.HasJsonValueOneOf)]
    public static AssertionResult HasJsonValueOneOf(this string json, string path, long[] candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        return JsonStringSource.Assert(json, root => HasJsonValueOneOf(root, path, candidates));
    }

    /// <summary>Asserts the value at <paramref name="path"/> is a 64-bit integer equal to any of
    /// <paramref name="candidates"/>, encoded as either a JSON number or a JSON integer literal in a string
    /// (parsed with <see cref="CultureInfo.InvariantCulture"/>).</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>message.sequence</c>.</param>
    /// <param name="candidates">The acceptable 64-bit integer values.</param>
    [GenerateAssertion(ExpectationMessage = Expectations.HasJsonValueOneOf)]
    public static AssertionResult HasJsonValueOneOf(this JsonElement element, string path, long[] candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        return JsonElementSource.Assert(element, root =>
        {
            var resolution = JsonPath.Resolve(root, path);
            return resolution.Found && JsonValueComparison.MatchesAny(resolution.Element, candidates)
                ? AssertionResult.Passed
                : AssertionResult.Failed(JsonFailureMessage.FoundInteger(resolution, "candidates: " + FormatCandidates(candidates)));
        });
    }

    /// <summary>Asserts the value at <paramref name="path"/> is an unsigned 64-bit integer equal to
    /// any of <paramref name="candidates"/>, encoded as either a JSON number or a JSON integer literal in a string
    /// (parsed with <see cref="CultureInfo.InvariantCulture"/>), for System.Text.Json or
    /// Protobuf-style <c>uint64</c> values.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>message.sequence</c>.</param>
    /// <param name="candidates">The acceptable unsigned 64-bit integer values.</param>
    [GenerateAssertion(ExpectationMessage = Expectations.HasJsonValueOneOf)]
    public static AssertionResult HasJsonValueOneOf(this string json, string path, ulong[] candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        return JsonStringSource.Assert(json, root => HasJsonValueOneOf(root, path, candidates));
    }

    /// <summary>Asserts the value at <paramref name="path"/> is an unsigned 64-bit integer equal to
    /// any of <paramref name="candidates"/>, encoded as either a JSON number or a JSON integer literal in a string
    /// (parsed with <see cref="CultureInfo.InvariantCulture"/>).</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>message.sequence</c>.</param>
    /// <param name="candidates">The acceptable unsigned 64-bit integer values.</param>
    [GenerateAssertion(ExpectationMessage = Expectations.HasJsonValueOneOf)]
    public static AssertionResult HasJsonValueOneOf(this JsonElement element, string path, ulong[] candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        return JsonElementSource.Assert(element, root =>
        {
            var resolution = JsonPath.Resolve(root, path);
            return resolution.Found && JsonValueComparison.MatchesAny(resolution.Element, candidates)
                ? AssertionResult.Passed
                : AssertionResult.Failed(JsonFailureMessage.FoundInteger(resolution, "candidates: " + FormatCandidates(candidates)));
        });
    }

    /// <summary>Asserts the value at <paramref name="path"/> is a JSON string whose text
    /// parses as <typeparamref name="T"/> via <see cref="IParsable{T}.TryParse(string, IFormatProvider, out T)"/>
    /// against <see cref="CultureInfo.InvariantCulture"/>. Covers the "value parses as
    /// <see cref="Guid"/> / <see cref="DateTimeOffset"/> / <see cref="TimeSpan"/>" pattern without
    /// committing to a particular parser per call site.</summary>
    /// <typeparam name="T">The target type implementing <see cref="IParsable{T}"/>.</typeparam>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>order.id</c>.</param>
    [GenerateAssertion(ExpectationMessage = Expectations.HasJsonValueParsableAs)]
    public static AssertionResult HasJsonValueParsableAs<T>(this string json, string path)
        where T : IParsable<T>
        => JsonStringSource.Assert(json, root => HasJsonValueParsableAs<T>(root, path));

    /// <summary>Asserts the value at <paramref name="path"/> is a JSON string whose text
    /// parses as <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The target type implementing <see cref="IParsable{T}"/>.</typeparam>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>order.id</c>.</param>
    [GenerateAssertion(ExpectationMessage = Expectations.HasJsonValueParsableAs)]
    public static AssertionResult HasJsonValueParsableAs<T>(this JsonElement element, string path)
        where T : IParsable<T>
    {
        return JsonElementSource.Assert(element, root =>
        {
            var resolution = JsonPath.Resolve(root, path);
            if (!resolution.Found || resolution.Element.ValueKind is not JsonValueKind.String)
            {
                return AssertionResult.Failed(JsonFailureMessage.Found(resolution));
            }

            // The generator drops the type argument from the call line, so the failure names the type.
            return T.TryParse(resolution.Element.GetString(), CultureInfo.InvariantCulture, out _)
                ? AssertionResult.Passed
                : AssertionResult.Failed(JsonFailureMessage.ValueNotParsed(resolution.Element, typeof(T).Name));
        });
    }

    /// <summary>Throws when <paramref name="expected"/> is NaN or an infinity, which no JSON number can be.</summary>
    /// <param name="expected">The expected value of a <see cref="double"/> overload.</param>
    internal static void RequireFinite(double expected)
    {
        if (!double.IsFinite(expected))
        {
            throw new ArgumentOutOfRangeException(nameof(expected), expected, NonFiniteExpectation);
        }
    }

    /// <summary>Throws when any of <paramref name="candidates"/> is NaN or an infinity.</summary>
    /// <param name="candidates">The candidates of a <see cref="double"/> one-of overload.</param>
    internal static void RequireFinite(double[] candidates)
    {
        if (!Array.TrueForAll(candidates, double.IsFinite))
        {
            throw new ArgumentOutOfRangeException(nameof(candidates), NonFiniteExpectation);
        }
    }

    /// <summary>Formats a one-of candidate list for failure messages as JSON literals: <c>"a", "b"</c>
    /// for strings and <c>1, 2</c> for numerics. Strings are JSON-escaped so a candidate
    /// containing a quote, backslash, or control character renders unambiguously.</summary>
    private static string FormatCandidates(string[] candidates)
    {
        var parts = new string[candidates.Length];
        for (var i = 0; i < candidates.Length; i++)
        {
            parts[i] = JsonFailureMessage.Quote(candidates[i]);
        }

        return string.Join(", ", parts);
    }

    /// <summary>Formats a one-of candidate list of numerics.</summary>
    private static string FormatCandidates(double[] candidates)
    {
        var parts = new string[candidates.Length];
        for (var i = 0; i < candidates.Length; i++)
        {
            parts[i] = candidates[i].ToString(CultureInfo.InvariantCulture);
        }

        return string.Join(", ", parts);
    }

    /// <summary>Formats a one-of candidate list of 32-bit integers. The candidates are unquoted:
    /// they denote the integer value, matched whether the JSON encodes it as a number or as a JSON
    /// integer literal in a string.</summary>
    private static string FormatCandidates(int[] candidates)
    {
        var parts = new string[candidates.Length];
        for (var i = 0; i < candidates.Length; i++)
        {
            parts[i] = candidates[i].ToString(CultureInfo.InvariantCulture);
        }

        return string.Join(", ", parts);
    }

    /// <summary>Formats a one-of candidate list of unsigned 32-bit integers. The candidates are
    /// unquoted: they denote the integer value, matched whether the JSON encodes it as a number or a
    /// JSON integer literal in a string.</summary>
    private static string FormatCandidates(uint[] candidates)
    {
        var parts = new string[candidates.Length];
        for (var i = 0; i < candidates.Length; i++)
        {
            parts[i] = candidates[i].ToString(CultureInfo.InvariantCulture);
        }

        return string.Join(", ", parts);
    }

    /// <summary>Formats a one-of candidate list of 64-bit integers. The candidates are unquoted:
    /// they denote the integer value, matched whether the JSON encodes it as a number or as a JSON
    /// integer literal in a string.</summary>
    private static string FormatCandidates(long[] candidates)
    {
        var parts = new string[candidates.Length];
        for (var i = 0; i < candidates.Length; i++)
        {
            parts[i] = candidates[i].ToString(CultureInfo.InvariantCulture);
        }

        return string.Join(", ", parts);
    }

    /// <summary>Formats a one-of candidate list of unsigned 64-bit integers. The candidates are
    /// unquoted: they denote the integer value, matched whether the JSON encodes it as a number or a
    /// JSON integer literal in a string.</summary>
    private static string FormatCandidates(ulong[] candidates)
    {
        var parts = new string[candidates.Length];
        for (var i = 0; i < candidates.Length; i++)
        {
            parts[i] = candidates[i].ToString(CultureInfo.InvariantCulture);
        }

        return string.Join(", ", parts);
    }
}
