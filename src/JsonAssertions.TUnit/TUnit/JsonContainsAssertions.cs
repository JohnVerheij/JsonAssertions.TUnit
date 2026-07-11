using System;
using System.Text.Json;
using JsonAssertions;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace JsonAssertions.TUnit;

/// <summary>
/// Fluent TUnit entry points for asserting that a JSON document <em>contains</em> an expected
/// subset: every property in the expected document must be present in the actual document with an
/// equivalent value (recursively), while the actual document may carry additional properties. This
/// is the subset counterpart of <see cref="JsonEquivalenceAssertions.IsEquivalentJsonTo(string, string)"/>
/// (full equality); it collapses a block of per-property <c>HasJsonProperty</c> / <c>HasJsonValue</c>
/// checks into one declarative assertion whose failure lists every field that is missing or wrong.
/// Matching is independent of object-property order and of the lexical form of numbers (<c>1</c>,
/// <c>1.0</c>, and <c>1e0</c> are equal); an expected array asserts a positional prefix (the actual
/// array may be longer) unless <see cref="JsonEquivalenceOptions.IgnoreArrayOrder"/> is enabled
/// through the configure callback, and any path registered with
/// <see cref="JsonEquivalenceOptions.IgnorePath"/> is excluded. Each method is generated into a
/// TUnit assertion chain via the <c>[GenerateAssertion]</c> source generator and delegates to
/// <see cref="JsonEquivalence.ContainsAll(JsonElement, JsonElement, JsonEquivalenceOptions)"/> in
/// the framework-agnostic core. Both a JSON <see cref="string"/> and a <see cref="JsonElement"/> are
/// accepted as the asserted value; the expected subset is supplied as a JSON <see cref="string"/>.
/// </summary>
public static class JsonContainsAssertions
{
    /// <summary>Asserts that the JSON string contains <paramref name="expectedSubset"/> as a subset.</summary>
    /// <param name="actual">The actual JSON document text.</param>
    /// <param name="expectedSubset">The expected subset document text.</param>
    [GenerateAssertion]
    public static AssertionResult ContainsJson(this string actual, string expectedSubset)
        => ContainsJson(actual, expectedSubset, configure: null);

    /// <summary>Asserts that the JSON string contains <paramref name="expectedSubset"/> as a subset,
    /// with comparison options (ignored paths, order-insensitive arrays) set by
    /// <paramref name="configure"/>.</summary>
    /// <param name="actual">The actual JSON document text.</param>
    /// <param name="expectedSubset">The expected subset document text.</param>
    /// <param name="configure">A callback that sets comparison options. May be
    /// <see langword="null"/> for the defaults.</param>
    [GenerateAssertion]
    public static AssertionResult ContainsJson(this string actual, string expectedSubset, Action<JsonEquivalenceOptions>? configure)
    {
        ArgumentNullException.ThrowIfNull(expectedSubset);
        var options = BuildOptions(configure);
        return JsonStringSource.Assert(actual, root => ContainsAgainst(root, expectedSubset, options));
    }

    /// <summary>Asserts that the JSON element contains <paramref name="expectedSubset"/> as a subset.</summary>
    /// <param name="actual">The actual JSON element.</param>
    /// <param name="expectedSubset">The expected subset document text.</param>
    [GenerateAssertion]
    public static AssertionResult ContainsJson(this JsonElement actual, string expectedSubset)
        => ContainsJson(actual, expectedSubset, configure: null);

    /// <summary>Asserts that the JSON element contains <paramref name="expectedSubset"/> as a subset,
    /// with comparison options (ignored paths, order-insensitive arrays) set by
    /// <paramref name="configure"/>.</summary>
    /// <param name="actual">The actual JSON element.</param>
    /// <param name="expectedSubset">The expected subset document text.</param>
    /// <param name="configure">A callback that sets comparison options. May be
    /// <see langword="null"/> for the defaults.</param>
    [GenerateAssertion]
    public static AssertionResult ContainsJson(this JsonElement actual, string expectedSubset, Action<JsonEquivalenceOptions>? configure)
    {
        ArgumentNullException.ThrowIfNull(expectedSubset);
        var options = BuildOptions(configure);
        return ContainsAgainst(actual, expectedSubset, options);
    }

    /// <summary>Builds the comparison options, applying the optional configure callback. Shared with
    /// <c>HttpResponseMessageAssertions.ContainsJson</c> so the options wiring exists once.</summary>
    /// <param name="configure">The optional configurator.</param>
    /// <returns>The built options.</returns>
    internal static JsonEquivalenceOptions BuildOptions(Action<JsonEquivalenceOptions>? configure)
    {
        var options = new JsonEquivalenceOptions();
        configure?.Invoke(options);
        return options;
    }

    /// <summary>Parses the expected subset and runs the subset comparison against <paramref name="actual"/>,
    /// mapping a malformed expected document to an explained failure and any differences to the
    /// contains-failure rendering.</summary>
    /// <param name="actual">The actual element.</param>
    /// <param name="expectedSubset">The expected subset document text.</param>
    /// <param name="options">The comparison options.</param>
    /// <returns>The assertion result.</returns>
    internal static AssertionResult ContainsAgainst(JsonElement actual, string expectedSubset, JsonEquivalenceOptions options)
    {
        JsonDocument expectedDocument;
        try
        {
            expectedDocument = JsonDocument.Parse(expectedSubset);
        }
        catch (JsonException exception)
        {
            return AssertionResult.Failed(JsonFailureMessage.ExpectedParseFailure(exception));
        }

        using (expectedDocument)
        {
            var differences = JsonEquivalence.ContainsAll(expectedDocument.RootElement, actual, options);
            return differences.Count is 0
                ? AssertionResult.Passed
                : AssertionResult.Failed(JsonFailureMessage.ContainsMismatch(differences));
        }
    }
}
