using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text.Json;

namespace JsonAssertions;

/// <summary>
/// Framework-agnostic structural comparison of two JSON documents. Two documents are equivalent
/// when they carry the same values at the same paths, independent of object-property order and of
/// the lexical form of numbers (<c>1</c>, <c>1.0</c>, and <c>1e0</c> are equal). Arrays are
/// position-sensitive unless <see cref="JsonEquivalenceOptions.IgnoreArrayOrder"/> is enabled, and
/// any path registered with <see cref="JsonEquivalenceOptions.IgnorePath"/> is excluded from the
/// comparison. <see cref="Compare(JsonElement, JsonElement, JsonEquivalenceOptions)"/> returns the
/// first <see cref="JsonDifference"/> found, or <see langword="null"/> when the documents are
/// equivalent.
/// </summary>
public static class JsonEquivalence
{
    private const int MaxRenderedLength = 200;

    /// <summary>Compares two JSON document strings for structural equivalence.</summary>
    /// <param name="expected">The expected JSON document text.</param>
    /// <param name="actual">The actual JSON document text.</param>
    /// <param name="options">The comparison options.</param>
    /// <returns>The first difference found, or <see langword="null"/> when equivalent.</returns>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    /// <summary>
    /// Parses two JSON strings and compares them structurally.
    /// </summary>
    /// <param name="expected">The expected JSON as a string.</param>
    /// <param name="actual">The actual JSON string to compare against the expected.</param>
    /// <param name="options">Configuration that controls comparison behavior, such as which paths to ignore.</param>
    /// <returns>The first structural difference found, or null if the JSON documents are equivalent.</returns>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
    /// <exception cref="JsonException">Either argument is not valid JSON.</exception>
    public static JsonDifference? Compare(string expected, string actual, JsonEquivalenceOptions options)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(actual);
        ArgumentNullException.ThrowIfNull(options);

        using var expectedDocument = JsonDocument.Parse(expected);
        using var actualDocument = JsonDocument.Parse(actual);
        return Compare(expectedDocument.RootElement, actualDocument.RootElement, options);
    }

    /// <summary>Compares two parsed JSON elements for structural equivalence. The elements must stay
    /// valid for the call (their backing documents alive).</summary>
    /// <param name="expected">The expected JSON element.</param>
    /// <param name="actual">The actual JSON element.</param>
    /// <param name="options">The comparison options.</param>
    /// <returns>The first difference found, or <see langword="null"/> when equivalent.</returns>
    /// <summary>
    /// Compares two JSON elements structurally.
    /// </summary>
    /// <param name="expected">The expected JSON element.</param>
    /// <param name="actual">The actual JSON element.</param>
    /// <param name="options">Configuration for the comparison, including ignored paths and array ordering behavior.</param>
    /// <returns>The first structural difference found, or <see langword="null"/> if the elements are equivalent.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    public static JsonDifference? Compare(JsonElement expected, JsonElement actual, JsonEquivalenceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var ignored = ResolveIgnoredPaths(expected, actual, options);
        return CompareElement(expected, actual, string.Empty, options, ignored);
    }

    /// <summary>
    /// Resolves configured path patterns against both JSON documents to build a set of paths to ignore.
    /// </summary>
    /// <returns>A set of path prefixes to ignore during comparison.</returns>
    private static HashSet<string> ResolveIgnoredPaths(JsonElement expected, JsonElement actual, JsonEquivalenceOptions options)
    {
        var ignored = new HashSet<string>(StringComparer.Ordinal);
        foreach (var pattern in options.IgnoredPaths)
        {
            AddResolved(ignored, expected, pattern);
            AddResolved(ignored, actual, pattern);
        }

        return ignored;
    }

    /// <summary>
    /// Resolves a JSONPath pattern and adds all successful resolutions to the ignored paths set.
    /// </summary>
    private static void AddResolved(HashSet<string> ignored, JsonElement root, string pattern)
    {
        foreach (var resolution in JsonPath.ResolveAll(root, pattern))
        {
            if (resolution.Found)
            {
                ignored.Add(resolution.ResolvedPrefix);
            }
        }
    }

    /// <summary>
    /// Compares two JSON elements at the specified path, returning the first difference found.
    /// </summary>
    /// <param name="expected">The expected JSON element.</param>
    /// <param name="actual">The actual JSON element.</param>
    /// <param name="path">The JSON path to the element being compared, used for locating differences.</param>
    /// <param name="options">Configuration controlling comparison behavior (e.g., array order sensitivity).</param>
    /// <param name="ignored">Set of resolved JSON paths to exclude from comparison.</param>
    /// <returns>null if the elements are equivalent; otherwise the first JsonDifference detected.</returns>
    private static JsonDifference? CompareElement(
        JsonElement expected, JsonElement actual, string path, JsonEquivalenceOptions options, HashSet<string> ignored)
    {
        if (ignored.Contains(path))
        {
            return null;
        }

        if (expected.ValueKind != actual.ValueKind)
        {
            // True and False are distinct value kinds but the same JSON type, so a differing boolean
            // is a value difference, not a kind difference.
            var kind = IsBoolean(expected.ValueKind) && IsBoolean(actual.ValueKind)
                ? JsonDifferenceKind.Value
                : JsonDifferenceKind.Kind;
            return new JsonDifference(path, kind, RenderValue(expected), RenderValue(actual));
        }

        return expected.ValueKind switch
        {
            JsonValueKind.Object => CompareObject(expected, actual, path, options, ignored),
            JsonValueKind.Array => options.IgnoreArrayOrderEnabled
                ? CompareArrayUnordered(expected, actual, path, options, ignored)
                : CompareArrayOrdered(expected, actual, path, options, ignored),
            JsonValueKind.Number => NumbersEqual(expected, actual)
                ? null
                : new JsonDifference(path, JsonDifferenceKind.Value, RenderValue(expected), RenderValue(actual)),
            JsonValueKind.String => string.Equals(expected.GetString(), actual.GetString(), StringComparison.Ordinal)
                ? null
                : new JsonDifference(path, JsonDifferenceKind.Value, RenderValue(expected), RenderValue(actual)),
            _ => null, // True / False / Null: equal value kinds already imply equal values.
        };
    }

    /// <summary>
    /// Compares two JSON objects for structural equivalence.
    /// </summary>
    /// <param name="expected">The expected JSON object.</param>
    /// <param name="actual">The actual JSON object.</param>
    /// <param name="path">The JSON path to this object.</param>
    /// <param name="options">Comparison configuration and ignored path patterns.</param>
    /// <param name="ignored">Set of resolved paths to exclude from comparison.</param>
    /// <returns>The first structural difference found, or null if the objects are equivalent.</returns>
    private static JsonDifference? CompareObject(
        JsonElement expected, JsonElement actual, string path, JsonEquivalenceOptions options, HashSet<string> ignored)
    {
        foreach (var property in expected.EnumerateObject())
        {
            var childPath = ChildPath(path, property.Name);
            if (ignored.Contains(childPath))
            {
                continue;
            }

            if (!actual.TryGetProperty(property.Name, out var actualValue))
            {
                return new JsonDifference(childPath, JsonDifferenceKind.MissingProperty, RenderValue(property.Value), "absent");
            }

            var difference = CompareElement(property.Value, actualValue, childPath, options, ignored);
            if (difference is not null)
            {
                return difference;
            }
        }

        foreach (var property in actual.EnumerateObject())
        {
            var childPath = ChildPath(path, property.Name);
            if (!ignored.Contains(childPath) && !expected.TryGetProperty(property.Name, out _))
            {
                return new JsonDifference(childPath, JsonDifferenceKind.UnexpectedProperty, "absent", RenderValue(property.Value));
            }
        }

        return null;
    }

    /// <summary>
    /// Compares two JSON arrays in order, excluding ignored paths.
    /// </summary>
    /// <returns>The first JSON difference found, or null if the arrays are equivalent.</returns>
    private static JsonDifference? CompareArrayOrdered(
        JsonElement expected, JsonElement actual, string path, JsonEquivalenceOptions options, HashSet<string> ignored)
    {
        // Exclude whole elements registered as ignored paths (for example IgnorePath("a[1]") or the
        // wildcard "a[*]") before comparing, so an ignored element never trips a length difference and
        // a wildcard can suppress extra or missing elements. Element paths whose only ignored part is a
        // child (for example "a[*].t") keep the element and skip the child inside CompareElement.
        var expectedItems = NonIgnoredItems(expected, path, ignored);
        var actualItems = NonIgnoredItems(actual, path, ignored);
        if (expectedItems.Count != actualItems.Count)
        {
            return ArrayLengthDifference(path, expectedItems.Count, actualItems.Count);
        }

        for (var i = 0; i < expectedItems.Count; i++)
        {
            var difference = CompareElement(
                expectedItems[i].Element, actualItems[i].Element, ChildIndex(path, expectedItems[i].Index), options, ignored);
            if (difference is not null)
            {
                return difference;
            }
        }

        return null;
    }

    /// <summary>
    /// Compares two JSON arrays as unordered collections.
    /// </summary>
    /// <returns>
    /// `null` if equivalent when element order is disregarded, otherwise the first structural difference.
    /// </returns>
    private static JsonDifference? CompareArrayUnordered(
        JsonElement expected, JsonElement actual, string path, JsonEquivalenceOptions options, HashSet<string> ignored)
    {
        var expectedItems = NonIgnoredItems(expected, path, ignored);
        var actualPairs = NonIgnoredItems(actual, path, ignored);
        if (expectedItems.Count != actualPairs.Count)
        {
            return ArrayLengthDifference(path, expectedItems.Count, actualPairs.Count);
        }

        var actualItems = new List<JsonElement>(actualPairs.Count);
        foreach (var pair in actualPairs)
        {
            actualItems.Add(pair.Element);
        }

        var matched = new bool[actualItems.Count];
        foreach (var (index, expectedItem) in expectedItems)
        {
            var childPath = ChildIndex(path, index);
            if (!TryMatchUnordered(expectedItem, actualItems, matched, childPath, options, ignored))
            {
                return new JsonDifference(
                    childPath, JsonDifferenceKind.ArrayElementUnmatched, RenderValue(expectedItem), "no equivalent element in the actual array");
            }
        }

        return null;
    }

    /// <summary>Returns the array's elements paired with their original index, excluding any element
    /// whose own path is in the ignored set. Child-only ignores (for example a field on every element)
    /// <summary>
    /// Enumerates array elements, excluding those whose element paths are marked as ignored.
    /// </summary>
    /// <param name="array">The JSON array to enumerate.</param>
    /// <param name="path">The current path prefix to the array.</param>
    /// <param name="ignored">The set of paths to exclude.</param>
    /// <returns>A list of tuples with the original index and element for each non-ignored item.</returns>
    private static List<(int Index, JsonElement Element)> NonIgnoredItems(
        JsonElement array, string path, HashSet<string> ignored)
    {
        var items = new List<(int Index, JsonElement Element)>(array.GetArrayLength());
        var index = 0;
        foreach (var item in array.EnumerateArray())
        {
            if (!ignored.Contains(ChildIndex(path, index)))
            {
                items.Add((index, item));
            }

            index++;
        }

        return items;
    }

    /// <summary>
    /// Finds and marks an unmatched actual element that is equivalent to the expected element.
    /// </summary>
    /// <param name="matched">Boolean array tracking which actual elements have been matched. Marked true when an equivalent element is found.</param>
    /// <returns>`true` if an unmatched element was found and marked, `false` otherwise.</returns>
    private static bool TryMatchUnordered(
        JsonElement expectedItem, List<JsonElement> actualItems, bool[] matched, string childPath, JsonEquivalenceOptions options, HashSet<string> ignored)
    {
        for (var j = 0; j < actualItems.Count; j++)
        {
            if (!matched[j] && CompareElement(expectedItem, actualItems[j], childPath, options, ignored) is null)
            {
                matched[j] = true;
                return true;
            }
        }

        return false;
    }

    /// <summary>
            /// Creates a JsonDifference representing an array length mismatch.
            /// </summary>
            /// <param name="path">The path to the array in the JSON document.</param>
            /// <param name="expectedCount">The number of elements in the expected array.</param>
            /// <param name="actualCount">The number of elements in the actual array.</param>
            /// <returns>A JsonDifference for the array length mismatch.</returns>
            private static JsonDifference ArrayLengthDifference(string path, int expectedCount, int actualCount)
        => new(
            path,
            JsonDifferenceKind.ArrayLength,
            expectedCount.ToString(CultureInfo.InvariantCulture) + " element(s)",
            actualCount.ToString(CultureInfo.InvariantCulture) + " element(s)");

    /// <summary>
    /// Determines if two JSON numeric values are equivalent.
    /// </summary>
    /// <returns>`true` if the numbers are equivalent, `false` otherwise.</returns>
    private static bool NumbersEqual(JsonElement a, JsonElement b)
    {
        if (a.TryGetDecimal(out var da) && b.TryGetDecimal(out var db))
        {
            return da == db;
        }

        // Beyond decimal range: compare the literals in a canonical (sign, significand, exponent) form.
        // This keeps number-form independence (1e400 == 1E400 == 10e399) and, unlike a double compare,
        // does not equate distinct values that round to the same binary64 (for example 1e29 and
        // 1e29 + 1 both collapse to 1e29 as a double). Falls back to raw-text equality only if a literal
        // fails to canonicalize (an exponent beyond int range); the document already parsed as valid JSON.
        var rawA = a.GetRawText();
        var rawB = b.GetRawText();
        return TryCanonicalizeNumber(rawA, out var canonicalA)
            && TryCanonicalizeNumber(rawB, out var canonicalB)
            ? canonicalA.Equals(canonicalB)
            : string.Equals(rawA, rawB, StringComparison.Ordinal);
    }

    /// <summary>
    /// Parses a JSON number literal into a canonical <c>(sign, significand, exponent)</c> form: the
    /// significand is a non-negative <see cref="BigInteger"/> with trailing zeros stripped and the
    /// exponent adjusted, so two literals denoting the same value canonicalize identically regardless
    /// of exponent case, scientific-form normalization, or insignificant zeros. Zero canonicalizes to
    /// a positive sign with a zero significand and exponent.
    /// <summary>
    /// Canonicalizes a JSON numeric literal into a normalized form with separated sign, significand, and exponent.
    /// </summary>
    /// <param name="literal">The JSON numeric literal to canonicalize.</param>
    /// <param name="canonical">The canonical form with sign, significand, and exponent; undefined if the method returns false.</param>
    /// <returns>true if the literal is successfully canonicalized; false if the exponent cannot be parsed as an integer.</returns>
    private static bool TryCanonicalizeNumber(string literal, out (int Sign, BigInteger Significand, int Exponent) canonical)
    {
        canonical = default;

        var mantissa = literal;
        var exponent = 0;
        var eIndex = literal.IndexOfAny(['e', 'E']);
        if (eIndex >= 0)
        {
            if (!int.TryParse(literal.AsSpan(eIndex + 1), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out exponent))
                return false;
            mantissa = literal[..eIndex];
        }

        var sign = 1;
        if (mantissa.StartsWith('-'))
        {
            sign = -1;
            mantissa = mantissa[1..];
        }

        var dotIndex = mantissa.IndexOf('.', StringComparison.Ordinal);
        if (dotIndex >= 0)
        {
            var fractionLength = mantissa.Length - dotIndex - 1;
            exponent -= fractionLength;
            mantissa = string.Concat(mantissa.AsSpan(0, dotIndex), mantissa.AsSpan(dotIndex + 1));
        }

        // The mantissa is now a non-empty digit string (JSON guarantees a leading integer digit), so
        // Parse cannot throw. A zero can still reach here: NumbersEqual only canonicalizes when an
        // operand is non-finite as a double (for example zero compared against 1e400), so a zero
        // literal paired with an extreme-magnitude one is canonicalized. Guard it explicitly, both to
        // give zero a single canonical form (positive sign, regardless of a "-0" literal) and to keep
        // the trailing-zero loop terminating.
        var significand = BigInteger.Parse(mantissa, NumberStyles.None, CultureInfo.InvariantCulture);
        if (significand.IsZero)
        {
            canonical = (1, BigInteger.Zero, 0);
            return true;
        }

        while (significand % 10 == 0)
        {
            significand /= 10;
            exponent++;
        }

        canonical = (sign, significand, exponent);
        return true;
    }

    /// <summary>
/// Determines whether a JSON value kind represents a boolean.
/// </summary>
/// <returns>true if the kind is JsonValueKind.True or JsonValueKind.False, false otherwise.</returns>
private static bool IsBoolean(JsonValueKind kind) => kind is JsonValueKind.True or JsonValueKind.False;

    /// <summary>
        /// Constructs a child property path by appending a property name to a parent path.
        /// </summary>
        /// <returns>The property name appended to the parent path with a dot separator, or just the property name if the parent path is empty.</returns>
        private static string ChildPath(string path, string name)
        => path.Length is 0 ? name : string.Concat(path, ".", name);

    /// <summary>
        /// Constructs a child array element path by appending the index in bracket notation.
        /// </summary>
        /// <param name="path">The parent path.</param>
        /// <param name="index">The array element index.</param>
        /// <returns>The path with the index appended in bracket notation (e.g., "path[0]").</returns>
        private static string ChildIndex(string path, int index)
        => string.Concat(path, "[", index.ToString(CultureInfo.InvariantCulture), "]");

    /// <summary>
    /// Renders a JSON element as a string for use in difference messages.
    /// </summary>
    /// <returns>A formatted string representation of the element, with objects and arrays truncated, strings quoted, and other values rendered in their appropriate literal form.</returns>
    private static string RenderValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object or JsonValueKind.Array => Truncate(element.GetRawText()),
        JsonValueKind.String => "\"" + Truncate(element.GetString()!) + "\"",
        JsonValueKind.Number => element.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        _ => "null", // Null (Undefined cannot occur for a parsed element)
    };

    /// <summary>
        /// Truncates a string to the maximum rendered length and appends an ellipsis if truncated.
        /// </summary>
        /// <returns>The original string, or if longer than the maximum rendered length, the first portion up to that length followed by "...".</returns>
        private static string Truncate(string value)
        => value.Length <= MaxRenderedLength ? value : value[..MaxRenderedLength] + "...";
}
