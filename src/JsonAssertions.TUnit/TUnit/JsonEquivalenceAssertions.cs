using System;
using System.Text.Json;
using JsonAssertions;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace JsonAssertions.TUnit;

/// <summary>
/// Fluent TUnit entry points for asserting that a JSON document is structurally equivalent to an
/// expected one. Equivalence is independent of object-property order and of the lexical form of
/// numbers (<c>1</c>, <c>1.0</c>, and <c>1e0</c> are equal); arrays are position-sensitive unless
/// <see cref="JsonEquivalenceOptions.IgnoreArrayOrder"/> is enabled through the configure callback,
/// and any path registered with <see cref="JsonEquivalenceOptions.IgnorePath"/> is excluded. Each
/// method is generated into a TUnit assertion chain via the <c>[GenerateAssertion]</c> source
/// generator and delegates to <see cref="JsonEquivalence"/> in the framework-agnostic core. Both a
/// JSON <see cref="string"/> and a <see cref="JsonElement"/> are accepted as the asserted value; the
/// expected document is supplied as a JSON <see cref="string"/>.
/// </summary>
public static class JsonEquivalenceAssertions
{
    /// <summary>Asserts that the JSON string is structurally equivalent to <paramref name="expected"/>.</summary>
    /// <param name="actual">The actual JSON document text.</param>
    /// <summary>
        /// Asserts that the actual JSON is structurally equivalent to the expected JSON.
        /// </summary>
        /// <param name="expected">The expected JSON document text.</param>
        /// <returns>An AssertionResult that passes if the documents are equivalent, fails otherwise.</returns>
    [GenerateAssertion]
    public static AssertionResult IsEquivalentJsonTo(this string actual, string expected)
        => IsEquivalentJsonTo(actual, expected, configure: null);

    /// <summary>Asserts that the JSON string is structurally equivalent to <paramref name="expected"/>,
    /// with comparison options (ignored paths, order-insensitive arrays) set by
    /// <paramref name="configure"/>.</summary>
    /// <param name="actual">The actual JSON document text.</param>
    /// <param name="expected">The expected JSON document text.</param>
    /// <param name="configure">A callback that sets comparison options. May be
    /// <summary>
    /// Asserts that the actual JSON string is structurally equivalent to the expected JSON string.
    /// </summary>
    /// <param name="configure">Optional callback to configure the equivalence options. Pass null to use the default settings.</param>
    /// <returns>An AssertionResult indicating whether the JSON structures are equivalent.</returns>
    [GenerateAssertion]
    public static AssertionResult IsEquivalentJsonTo(this string actual, string expected, Action<JsonEquivalenceOptions>? configure)
    {
        ArgumentNullException.ThrowIfNull(expected);
        var options = BuildOptions(configure);
        return JsonStringSource.Assert(actual, root => CompareAgainst(root, expected, options));
    }

    /// <summary>Asserts that the JSON element is structurally equivalent to <paramref name="expected"/>.</summary>
    /// <param name="actual">The actual JSON element.</param>
    /// <summary>
        /// Asserts that a JSON element is structurally equivalent to an expected JSON document.
        /// </summary>
        /// <param name="expected">The expected JSON document text.</param>
        /// <returns>An AssertionResult indicating if the JSON documents are structurally equivalent.</returns>
    [GenerateAssertion]
    public static AssertionResult IsEquivalentJsonTo(this JsonElement actual, string expected)
        => IsEquivalentJsonTo(actual, expected, configure: null);

    /// <summary>Asserts that the JSON element is structurally equivalent to <paramref name="expected"/>,
    /// with comparison options (ignored paths, order-insensitive arrays) set by
    /// <paramref name="configure"/>.</summary>
    /// <param name="actual">The actual JSON element.</param>
    /// <param name="expected">The expected JSON document text.</param>
    /// <param name="configure">A callback that sets comparison options. May be
    /// <summary>
    /// Asserts that the JSON element is structurally equivalent to an expected JSON document.
    /// </summary>
    /// <param name="actual">The JSON element to verify.</param>
    /// <param name="expected">The expected JSON document as a string.</param>
    /// <param name="configure">An optional callback to configure equivalence options; pass <see langword="null"/> for the defaults.</param>
    /// <returns>An <see cref="AssertionResult"/> indicating whether the assertion passed or failed.</returns>
    [GenerateAssertion]
    public static AssertionResult IsEquivalentJsonTo(this JsonElement actual, string expected, Action<JsonEquivalenceOptions>? configure)
    {
        ArgumentNullException.ThrowIfNull(expected);
        var options = BuildOptions(configure);
        return CompareAgainst(actual, expected, options);
    }

    private static JsonEquivalenceOptions BuildOptions(Action<JsonEquivalenceOptions>? configure)
    {
        var options = new JsonEquivalenceOptions();
        configure?.Invoke(options);
        return options;
    }

    private static AssertionResult CompareAgainst(JsonElement actual, string expected, JsonEquivalenceOptions options)
    {
        JsonDocument expectedDocument;
        try
        {
            expectedDocument = JsonDocument.Parse(expected);
        }
        catch (JsonException exception)
        {
            return AssertionResult.Failed(JsonFailureMessage.ExpectedParseFailure(exception));
        }

        using (expectedDocument)
        {
            var difference = JsonEquivalence.Compare(expectedDocument.RootElement, actual, options);
            return difference is null
                ? AssertionResult.Passed
                : AssertionResult.Failed(JsonFailureMessage.EquivalenceMismatch(difference));
        }
    }
}
