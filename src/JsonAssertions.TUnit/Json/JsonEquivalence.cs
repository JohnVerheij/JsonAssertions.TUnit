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
/// equivalent. <see cref="ContainsAll(JsonElement, JsonElement, JsonEquivalenceOptions)"/> performs
/// a one-directional subset comparison (the actual document may carry properties beyond the
/// expected set) and returns every difference rather than only the first.
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
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    public static JsonDifference? Compare(JsonElement expected, JsonElement actual, JsonEquivalenceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var ignored = ResolveIgnoredPaths(expected, actual, options);
        return CompareElement(expected, actual, string.Empty, options, ignored, subset: false, sink: null);
    }

    /// <summary>Determines whether <paramref name="actual"/> contains <paramref name="expected"/> as
    /// a subset: every property in the expected document must be present in the actual document with
    /// an equivalent value (recursively), while the actual document may carry additional properties.
    /// Every difference is collected, not only the first.</summary>
    /// <param name="expected">The expected subset document text.</param>
    /// <param name="actual">The actual JSON document text.</param>
    /// <param name="options">The comparison options.</param>
    /// <returns>Every difference found; an empty list when <paramref name="actual"/> contains the subset.</returns>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    /// <exception cref="JsonException">Either argument is not valid JSON.</exception>
    public static IReadOnlyList<JsonDifference> ContainsAll(string expected, string actual, JsonEquivalenceOptions options)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(actual);
        ArgumentNullException.ThrowIfNull(options);

        using var expectedDocument = JsonDocument.Parse(expected);
        using var actualDocument = JsonDocument.Parse(actual);
        return ContainsAll(expectedDocument.RootElement, actualDocument.RootElement, options);
    }

    /// <summary>Determines whether <paramref name="actual"/> contains <paramref name="expected"/> as
    /// a subset (see <see cref="ContainsAll(string, string, JsonEquivalenceOptions)"/>), over parsed
    /// elements. The elements must stay valid for the call (their backing documents alive).</summary>
    /// <param name="expected">The expected subset element.</param>
    /// <param name="actual">The actual JSON element.</param>
    /// <param name="options">The comparison options.</param>
    /// <returns>Every difference found; an empty list when <paramref name="actual"/> contains the subset.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<JsonDifference> ContainsAll(JsonElement expected, JsonElement actual, JsonEquivalenceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var ignored = ResolveIgnoredPaths(expected, actual, options);
        var sink = new List<JsonDifference>();
        CompareElement(expected, actual, string.Empty, options, ignored, subset: true, sink);
        return sink;
    }

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

    /// <summary>Records <paramref name="difference"/>: appends it to <paramref name="sink"/> and
    /// signals "continue" (collect-all mode) when a sink is supplied, otherwise signals "stop and
    /// return this difference" (first-difference mode). Returns the difference to return, or
    /// <see langword="null"/> to continue.</summary>
    private static JsonDifference? Report(JsonDifference difference, List<JsonDifference>? sink)
    {
        if (sink is null)
        {
            return difference;
        }

        sink.Add(difference);
        return null;
    }

    private static JsonDifference? CompareElement(
        JsonElement expected, JsonElement actual, string path, JsonEquivalenceOptions options, HashSet<string> ignored, bool subset, List<JsonDifference>? sink)
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
            return Report(new JsonDifference(path, kind, RenderValue(expected), RenderValue(actual)), sink);
        }

        return expected.ValueKind switch
        {
            JsonValueKind.Object => CompareObject(expected, actual, path, options, ignored, subset, sink),
            JsonValueKind.Array => options.IgnoreArrayOrderEnabled
                ? CompareArrayUnordered(expected, actual, path, options, ignored, subset, sink)
                : CompareArrayOrdered(expected, actual, path, options, ignored, subset, sink),
            JsonValueKind.Number => NumbersEqual(expected, actual)
                ? null
                : Report(new JsonDifference(path, JsonDifferenceKind.Value, RenderValue(expected), RenderValue(actual)), sink),
            JsonValueKind.String => string.Equals(expected.GetString(), actual.GetString(), StringComparison.Ordinal)
                ? null
                : Report(new JsonDifference(path, JsonDifferenceKind.Value, RenderValue(expected), RenderValue(actual)), sink),
            _ => null, // True / False / Null: equal value kinds already imply equal values.
        };
    }

    private static JsonDifference? CompareObject(
        JsonElement expected, JsonElement actual, string path, JsonEquivalenceOptions options, HashSet<string> ignored, bool subset, List<JsonDifference>? sink)
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
                var missing = Report(new JsonDifference(childPath, JsonDifferenceKind.MissingProperty, RenderValue(property.Value), "absent"), sink);
                if (missing is not null)
                {
                    return missing;
                }

                continue;
            }

            var difference = CompareElement(property.Value, actualValue, childPath, options, ignored, subset, sink);
            if (difference is not null)
            {
                return difference;
            }
        }

        // The reverse pass (flagging actual-only properties) applies to full equivalence only; a
        // subset match permits the actual document to carry properties beyond the expected set.
        if (!subset)
        {
            foreach (var property in actual.EnumerateObject())
            {
                var childPath = ChildPath(path, property.Name);
                if (!ignored.Contains(childPath) && !expected.TryGetProperty(property.Name, out _))
                {
                    var unexpected = Report(new JsonDifference(childPath, JsonDifferenceKind.UnexpectedProperty, "absent", RenderValue(property.Value)), sink);
                    if (unexpected is not null)
                    {
                        return unexpected;
                    }
                }
            }
        }

        return null;
    }

    private static JsonDifference? CompareArrayOrdered(
        JsonElement expected, JsonElement actual, string path, JsonEquivalenceOptions options, HashSet<string> ignored, bool subset, List<JsonDifference>? sink)
    {
        // Exclude whole elements registered as ignored paths (for example IgnorePath("a[1]") or the
        // wildcard "a[*]") before comparing, so an ignored element never trips a length difference and
        // a wildcard can suppress extra or missing elements. Element paths whose only ignored part is a
        // child (for example "a[*].t") keep the element and skip the child inside CompareElement.
        var expectedItems = NonIgnoredItems(expected, path, ignored);
        var actualItems = NonIgnoredItems(actual, path, ignored);

        // Full equivalence requires equal lengths; a subset requires only that the actual array has
        // at least as many elements, and compares the expected count element-by-element (trailing
        // actual elements are ignored).
        if (subset ? actualItems.Count < expectedItems.Count : expectedItems.Count != actualItems.Count)
        {
            var lengthDifference = Report(ArrayLengthDifference(path, expectedItems.Count, actualItems.Count, subset), sink);
            if (lengthDifference is not null)
            {
                return lengthDifference;
            }
        }

        var compareCount = Math.Min(expectedItems.Count, actualItems.Count);
        for (var i = 0; i < compareCount; i++)
        {
            var difference = CompareElement(
                expectedItems[i].Element, actualItems[i].Element, ChildIndex(path, expectedItems[i].Index), options, ignored, subset, sink);
            if (difference is not null)
            {
                return difference;
            }
        }

        return null;
    }

    private static JsonDifference? CompareArrayUnordered(
        JsonElement expected, JsonElement actual, string path, JsonEquivalenceOptions options, HashSet<string> ignored, bool subset, List<JsonDifference>? sink)
    {
        var expectedItems = NonIgnoredItems(expected, path, ignored);
        var actualPairs = NonIgnoredItems(actual, path, ignored);

        // Full equivalence requires equal lengths (bijection); a subset requires only that each
        // expected element has a distinct equivalent in the actual array, which may be larger.
        if (!subset && expectedItems.Count != actualPairs.Count)
        {
            var lengthDifference = Report(ArrayLengthDifference(path, expectedItems.Count, actualPairs.Count, subset: false), sink);
            if (lengthDifference is not null)
            {
                return lengthDifference;
            }
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
            if (!TryMatchUnordered(expectedItem, actualItems, matched, childPath, options, ignored, subset))
            {
                var unmatched = Report(
                    new JsonDifference(childPath, JsonDifferenceKind.ArrayElementUnmatched, RenderValue(expectedItem), "no equivalent element in the actual array"), sink);
                if (unmatched is not null)
                {
                    return unmatched;
                }
            }
        }

        return null;
    }

    /// <summary>Returns the array's elements paired with their original index, excluding any element
    /// whose own path is in the ignored set. Child-only ignores (for example a field on every element)
    /// keep the element; only an element-level ignore removes it.</summary>
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

    private static bool TryMatchUnordered(
        JsonElement expectedItem, List<JsonElement> actualItems, bool[] matched, string childPath, JsonEquivalenceOptions options, HashSet<string> ignored, bool subset)
    {
        for (var j = 0; j < actualItems.Count; j++)
        {
            // Probe each candidate with first-difference semantics and no sink: a probe that reported
            // into the real sink would record spurious differences for candidates that simply are not
            // this element's match. Only a genuine no-match is reported by the caller.
            if (!matched[j] && CompareElement(expectedItem, actualItems[j], childPath, options, ignored, subset, sink: null) is null)
            {
                matched[j] = true;
                return true;
            }
        }

        return false;
    }

    private static JsonDifference ArrayLengthDifference(string path, int expectedCount, int actualCount, bool subset)
        => new(
            path,
            JsonDifferenceKind.ArrayLength,
            (subset ? "at least " : string.Empty) + expectedCount.ToString(CultureInfo.InvariantCulture) + " element(s)",
            actualCount.ToString(CultureInfo.InvariantCulture) + " element(s)");

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
    /// </summary>
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

    private static bool IsBoolean(JsonValueKind kind) => kind is JsonValueKind.True or JsonValueKind.False;

    private static string ChildPath(string path, string name)
        => path.Length is 0 ? name : string.Concat(path, ".", name);

    private static string ChildIndex(string path, int index)
        => string.Concat(path, "[", index.ToString(CultureInfo.InvariantCulture), "]");

    private static string RenderValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object or JsonValueKind.Array => Truncate(element.GetRawText()),
        JsonValueKind.String => "\"" + Truncate(element.GetString()!) + "\"",
        JsonValueKind.Number => element.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        _ => "null", // Null (Undefined cannot occur for a parsed element)
    };

    private static string Truncate(string value)
        => value.Length <= MaxRenderedLength ? value : value[..MaxRenderedLength] + "...";
}
