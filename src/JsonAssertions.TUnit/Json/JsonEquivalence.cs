using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;
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

        using var expectedDocument = JsonText.Parse(expected);
        using var actualDocument = JsonText.Parse(actual);
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

        using var expectedDocument = JsonText.Parse(expected);
        using var actualDocument = JsonText.Parse(actual);
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

        return expected.ValueKind == actual.ValueKind
            ? CompareSameKind(expected, actual, path, options, ignored, subset, sink)
            : Report(KindDifference(expected, actual, path), sink);
    }

    /// <summary>The difference between two elements of different value kinds. True and False are distinct value
    /// kinds but the same JSON type, so a differing boolean is a value difference, not a kind difference.</summary>
    private static JsonDifference KindDifference(JsonElement expected, JsonElement actual, string path)
    {
        var kind = IsBoolean(expected.ValueKind) && IsBoolean(actual.ValueKind)
            ? JsonDifferenceKind.Value
            : JsonDifferenceKind.Kind;
        return new JsonDifference(path, kind, RenderValue(expected), RenderValue(actual));
    }

    private static JsonDifference? CompareSameKind(
        JsonElement expected, JsonElement actual, string path, JsonEquivalenceOptions options, HashSet<string> ignored, bool subset, List<JsonDifference>? sink)
        => expected.ValueKind switch
        {
            JsonValueKind.Object => CompareObject(expected, actual, path, options, ignored, subset, sink),
            JsonValueKind.Array => CompareArray(expected, actual, path, options, ignored, subset, sink),
            JsonValueKind.Number or JsonValueKind.String => CompareScalar(expected, actual, path, sink),
            _ => null, // True / False / Null: equal value kinds already imply equal values.
        };

    private static JsonDifference? CompareArray(
        JsonElement expected, JsonElement actual, string path, JsonEquivalenceOptions options, HashSet<string> ignored, bool subset, List<JsonDifference>? sink)
        => options.IgnoreArrayOrderEnabled
            ? CompareArrayUnordered(expected, actual, path, options, ignored, subset, sink)
            : CompareArrayOrdered(expected, actual, path, options, ignored, subset, sink);

    /// <summary>Compares two numbers or two strings of the same value kind.</summary>
    private static JsonDifference? CompareScalar(JsonElement expected, JsonElement actual, string path, List<JsonDifference>? sink)
    {
        var equal = expected.ValueKind is JsonValueKind.Number
            ? NumbersEqual(expected, actual)
            : string.Equals(expected.GetString(), actual.GetString(), StringComparison.Ordinal);
        return equal
            ? null
            : Report(new JsonDifference(path, JsonDifferenceKind.Value, RenderValue(expected), RenderValue(actual)), sink);
    }

    private static JsonDifference? CompareObject(
        JsonElement expected, JsonElement actual, string path, JsonEquivalenceOptions options, HashSet<string> ignored, bool subset, List<JsonDifference>? sink)
    {
        // The reverse pass (flagging actual-only properties) applies to full equivalence only; a
        // subset match permits the actual document to carry properties beyond the expected set.
        return CompareExpectedProperties(expected, actual, path, options, ignored, subset, sink)
            ?? (subset ? null : CompareUnexpectedProperties(expected, actual, path, ignored, sink));
    }

    private static JsonDifference? CompareExpectedProperties(
        JsonElement expected, JsonElement actual, string path, JsonEquivalenceOptions options, HashSet<string> ignored, bool subset, List<JsonDifference>? sink)
    {
        foreach (var property in expected.EnumerateObject())
        {
            var difference = CompareProperty(property, actual, path, options, ignored, subset, sink);
            if (difference is not null)
            {
                return difference;
            }
        }

        return null;
    }

    /// <summary>Compares one expected property with the actual object: skipped when its path is ignored, reported
    /// when the actual object lacks it, compared recursively otherwise.</summary>
    private static JsonDifference? CompareProperty(
        JsonProperty property, JsonElement actual, string path, JsonEquivalenceOptions options, HashSet<string> ignored, bool subset, List<JsonDifference>? sink)
    {
        var childPath = ChildPath(path, property.Name);
        if (ignored.Contains(childPath))
        {
            return null;
        }

        return actual.TryGetProperty(property.Name, out var actualValue)
            ? CompareElement(property.Value, actualValue, childPath, options, ignored, subset, sink)
            : Report(new JsonDifference(childPath, JsonDifferenceKind.MissingProperty, RenderValue(property.Value), "absent"), sink);
    }

    private static JsonDifference? CompareUnexpectedProperties(
        JsonElement expected, JsonElement actual, string path, HashSet<string> ignored, List<JsonDifference>? sink)
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

        // Maximum bipartite matching (see TryAugment): a first-fit greedy assignment is wrong for a
        // subset match, because an actual element that satisfies an early expected item may be the only
        // one that can satisfy a later, more specific one.
        var matchedBy = new int[actualItems.Count];
        Array.Fill(matchedBy, -1);

        for (var expectedIndex = 0; expectedIndex < expectedItems.Count; expectedIndex++)
        {
            var visited = new bool[actualItems.Count];
            if (TryAugment(expectedIndex, expectedItems, actualItems, matchedBy, visited, path, options, ignored, subset))
            {
                continue;
            }

            var (index, expectedItem) = expectedItems[expectedIndex];
            var unmatched = Report(
                new JsonDifference(ChildIndex(path, index), JsonDifferenceKind.ArrayElementUnmatched, RenderValue(expectedItem), "no equivalent element in the actual array"), sink);
            if (unmatched is not null)
            {
                return unmatched;
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

    /// <summary>
    /// Kuhn's augmenting-path step for maximum bipartite matching between the expected and actual array
    /// elements: tries to match the expected element at <paramref name="expectedIndex"/>, re-assigning
    /// already-matched actual elements along an augmenting path when that frees a candidate.
    /// </summary>
    /// <remarks>
    /// A first-fit greedy assignment is sufficient for full equivalence, because equality is symmetric
    /// and transitive: two actual elements that both equal one expected element are equal to each other,
    /// so which one is taken cannot matter to a later expected element. Subset containment has no such
    /// property. An actual element may satisfy a broad expected element while being the <em>only</em> one
    /// that can satisfy a later, more specific one, so a greedy choice can strand a satisfiable expected
    /// element and report a false mismatch. Searching for an augmenting path finds a complete assignment
    /// whenever one exists, so an expected element is reported unmatched only when it genuinely is.
    /// </remarks>
    /// <param name="expectedIndex">The expected element to match.</param>
    /// <param name="expectedItems">The expected elements paired with their original indices.</param>
    /// <param name="actualItems">The candidate actual elements.</param>
    /// <param name="matchedBy">For each actual element, the expected index currently matched to it, or -1.</param>
    /// <param name="visited">Actual elements already explored in this augmenting attempt (reset per attempt).</param>
    /// <param name="path">The array's path, for the child paths used when comparing elements.</param>
    /// <param name="options">The comparison options.</param>
    /// <param name="ignored">The resolved ignored paths.</param>
    /// <param name="subset">Whether elements are compared as subsets rather than for equality.</param>
    /// <returns><see langword="true"/> when the expected element was matched (possibly by re-assigning others).</returns>
    private static bool TryAugment(
        int expectedIndex,
        List<(int Index, JsonElement Element)> expectedItems,
        List<JsonElement> actualItems,
        int[] matchedBy,
        bool[] visited,
        string path,
        JsonEquivalenceOptions options,
        HashSet<string> ignored,
        bool subset)
    {
        var (index, expectedItem) = expectedItems[expectedIndex];
        var childPath = ChildIndex(path, index);

        for (var candidate = 0; candidate < actualItems.Count; candidate++)
        {
            if (visited[candidate])
            {
                continue;
            }

            // Probe each candidate with first-difference semantics and no sink: a probe that reported
            // into the real sink would record spurious differences for candidates that simply are not
            // this element's match. Only a genuine no-match is reported by the caller.
            if (CompareElement(expectedItem, actualItems[candidate], childPath, options, ignored, subset, sink: null) is not null)
            {
                continue;
            }

            visited[candidate] = true;
            if (matchedBy[candidate] is -1
                || TryAugment(matchedBy[candidate], expectedItems, actualItems, matchedBy, visited, path, options, ignored, subset))
            {
                matchedBy[candidate] = expectedIndex;
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

    /// <summary>Compares two JSON numbers exactly, by value: <c>1</c>, <c>1.0</c>, <c>10e-1</c> and <c>1E+0</c> are
    /// equal, and so are <c>-0</c> and <c>0</c>; no literal is rounded to a binary or decimal type first. Two integer
    /// literals that both fit in a <see cref="long"/> compare as longs, with no allocation; every other pair compares
    /// in normalized form.</summary>
    private static bool NumbersEqual(JsonElement a, JsonElement b)
        => a.TryGetInt64(out var left) && b.TryGetInt64(out var right)
            ? left == right
            : NormalizedNumber.From(a.GetRawText()) == NormalizedNumber.From(b.GetRawText());

    /// <summary>
    /// A JSON number literal in normalized form: the sign, the significant digits with no leading or trailing zero, and
    /// the exponent of the last of them. Two literals denote the same value exactly when their normalized forms are
    /// equal; zero has one form (positive, no digits). The form is built from the literal's text with no arithmetic on
    /// the significand and no power of ten: the significand costs time linear in its length, and only the exponent is
    /// parsed as a number, so a hostile literal (a long run of zeros, a huge exponent) cannot hang the comparison.
    /// </summary>
    /// <param name="Negative">Whether the value is below zero.</param>
    /// <param name="Digits">The significant digits.</param>
    /// <param name="Exponent">The power of ten of the last significant digit.</param>
    private readonly record struct NormalizedNumber(bool Negative, string Digits, BigInteger Exponent)
    {
        /// <summary>Normalizes <paramref name="literal"/>, a valid JSON number literal.</summary>
        /// <param name="literal">The literal, as <see cref="JsonElement.GetRawText"/> returns it.</param>
        /// <returns>The normalized form.</returns>
        public static NormalizedNumber From(string literal)
        {
            var text = literal.AsSpan();
            var negative = text[0] is '-';
            if (negative)
            {
                text = text[1..];
            }

            var exponentAt = text.IndexOfAny('e', 'E');
            var exponent = exponentAt < 0
                ? BigInteger.Zero
                : BigInteger.Parse(text[(exponentAt + 1)..], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
            var mantissa = exponentAt < 0 ? text : text[..exponentAt];
            var dot = mantissa.IndexOf('.');
            var digits = dot < 0 ? mantissa.ToString() : string.Concat(mantissa[..dot], mantissa[(dot + 1)..]);
            if (dot >= 0)
            {
                exponent -= mantissa.Length - dot - 1;
            }

            var significant = digits.AsSpan().TrimStart('0');
            var trimmed = significant.TrimEnd('0');
            return trimmed.IsEmpty
                ? new NormalizedNumber(Negative: false, string.Empty, BigInteger.Zero)
                : new NormalizedNumber(negative, trimmed.ToString(), exponent + (significant.Length - trimmed.Length));
        }
    }

    private static bool IsBoolean(JsonValueKind kind) => kind is JsonValueKind.True or JsonValueKind.False;

    private static string ChildPath(string path, string name)
        => path.Length is 0 ? name : string.Concat(path, ".", name);

    private static string ChildIndex(string path, int index)
        => string.Concat(path, "[", index.ToString(CultureInfo.InvariantCulture), "]");

    /// <summary>Renders a value as a JSON literal for a difference: a string quoted and escaped, an object or array
    /// written compactly on one line, each truncated when long.</summary>
    private static string RenderValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object or JsonValueKind.Array => Truncate(Compact(element.GetRawText())),
        JsonValueKind.String => JsonFailureMessage.Quote(Truncate(element.GetString()!)),
        JsonValueKind.Number => element.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        _ => "null", // Null (Undefined cannot occur for a parsed element)
    };

    /// <summary>Drops the white space between the tokens of a container's raw JSON, so an indented document renders on
    /// one line; strings, their escapes and their non-ASCII text stay exactly as written.</summary>
    private static string Compact(string raw)
    {
        var sb = new StringBuilder(raw.Length);
        var inString = false;
        var escaped = false;
        foreach (var c in raw)
        {
            if (inString)
            {
                sb.Append(c);
                inString = escaped || c != '"';
                escaped = !escaped && c == '\\';
            }
            else if (!char.IsWhiteSpace(c))
            {
                sb.Append(c);
                inString = c == '"';
            }
        }

        return sb.ToString();
    }

    private static string Truncate(string value)
        => value.Length <= MaxRenderedLength ? value : value[..MaxRenderedLength] + "...";
}
