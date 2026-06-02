using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace JsonAssertions;

/// <summary>
/// Framework-agnostic JSON path navigation primitives. This <c>JsonAssertions</c> namespace
/// holds the matching core; the TUnit-specific fluent entry points live in the sibling
/// <c>JsonAssertions.TUnit</c> namespace. Both ship in the single <c>JsonAssertions.TUnit</c>
/// package: the two-namespace split keeps the same consumer feel as the rest of the assertion
/// family (a framework-agnostic core plus a TUnit adapter) and future-proofs a package split
/// if the bare <c>JsonAssertions</c> identifier ever becomes available.
/// </summary>
/// <remarks>
/// <para>The path grammar is a small subset of JSONPath. Segments are either dot-separated
/// property names or zero-based bracket indices, both composable in the same path:</para>
/// <list type="bullet">
/// <item><c>user.address.city</c> &#x2014; nested property navigation.</item>
/// <item><c>items[0]</c> &#x2014; array element access.</item>
/// <item><c>objects[0].planData[0].pickPlanId</c> &#x2014; mixed nesting.</item>
/// <item><c>$</c> &#x2014; the root element itself; <c>$.user.name</c> equivalent to
/// <c>user.name</c>; <c>$[0]</c> equivalent to <c>[0]</c> against a root array.</item>
/// </list>
/// </remarks>
public static class JsonPath
{
    /// <summary>
    /// Resolves <paramref name="path"/> against <paramref name="element"/>, returning a
    /// <see cref="JsonPathResolution"/> that carries the resolved element on success and the
    /// failure-point context (how far it got, which segment failed, what kind of value blocked
    /// it) on failure.
    /// </summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>items[0].name</c>. A leading <c>$.</c> or bare <c>$</c> is
    /// accepted as the JSONPath root reference; <c>$</c> alone resolves to
    /// <paramref name="element"/> itself.</param>
    /// <returns>A <see cref="JsonPathResolution"/> describing the outcome.</returns>
    /// <exception cref="ArgumentException"><paramref name="path"/> is <see langword="null"/>,
    /// empty, whitespace, contains an empty property segment (a leading, trailing, or doubled
    /// dot), or contains a malformed index segment (an empty <c>[]</c>, a non-numeric or
    /// negative index, or an unclosed <c>[</c>).</exception>
    public static JsonPathResolution Resolve(JsonElement element, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var normalized = NormalizeRoot(path);
        return normalized.Length is 0
            ? JsonPathResolution.Resolved(element, string.Empty)
            : WalkSegments(element, normalized);
    }

    /// <summary>
    /// Reports whether a value exists at the given <paramref name="path"/> within
    /// <paramref name="element"/>. Equivalent to <c>Resolve(element, path).Found</c>.
    /// </summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices; see <see cref="Resolve"/>.</param>
    /// <returns><see langword="true"/> if every segment resolves; <see langword="false"/> if
    /// any segment is missing, out of range, or applied to a value of the wrong kind.</returns>
    /// <exception cref="ArgumentException"><paramref name="path"/> is invalid; see
    /// <see cref="Resolve"/>.</exception>
    public static bool Exists(JsonElement element, string path) => Resolve(element, path).Found;

    /// <summary>Walks the segment-by-segment grammar against <paramref name="root"/>,
    /// rebuilding the resolved prefix as it goes so a failure carries the longest-prefix
    /// context.</summary>
    private static JsonPathResolution WalkSegments(JsonElement root, string path)
    {
        var current = root;
        var prefix = new StringBuilder();
        var i = 0;

        while (i < path.Length)
        {
            var c = path[i];
            JsonPathResolution? failure;

            if (c is '[')
            {
                failure = ConsumeIndex(path, ref i, ref current, prefix);
            }
            else if (c is '.')
            {
                ConsumeDot(path, ref i, prefix.Length);
                failure = null;
            }
            else
            {
                failure = ConsumeProperty(path, ref i, ref current, prefix);
            }

            if (failure is not null)
            {
                return failure.Value;
            }
        }

        return JsonPathResolution.Resolved(current, prefix.ToString());
    }

    /// <summary>Consumes a <c>[N]</c> index from <paramref name="path"/>, navigating into
    /// the array on success or returning a failed resolution.</summary>
    private static JsonPathResolution? ConsumeIndex(
        string path,
        ref int i,
        ref JsonElement current,
        StringBuilder prefix)
    {
        var index = ParseIndex(path, ref i);
        var failedSegment = "[" + index.ToString(CultureInfo.InvariantCulture) + "]";

        if (current.ValueKind is not JsonValueKind.Array)
        {
            return JsonPathResolution.NotFound(prefix.ToString(), failedSegment, current.ValueKind);
        }

        if (index >= current.GetArrayLength())
        {
            return JsonPathResolution.NotFound(prefix.ToString(), failedSegment, current.ValueKind);
        }

        current = GetArrayElement(current, index);
        prefix.Append(failedSegment);

        // After ']', the only valid continuations are '.', '[', or end-of-path. A bare
        // property name (e.g. 'items[0]name') is rejected as a malformed path rather than
        // silently parsed as another property, so a missing '.' surfaces as an argument
        // error at the call site instead of a silent miss.
        if (i < path.Length && path[i] is not '.' and not '[')
        {
            throw new ArgumentException(
                "Path segment after ']' must be '.', '[', or end-of-path; a property name following an index requires a dot separator (use 'items[0].name', not 'items[0]name').",
                nameof(path));
        }

        return null;
    }

    /// <summary>Consumes a single <c>.</c> separator.</summary>
    private static void ConsumeDot(string path, ref int i, int prefixLength)
    {
        if (prefixLength is 0)
        {
            throw new ArgumentException(
                "Path begins with '.'; segments must be separated by single dots, with no leading dot.",
                nameof(path));
        }

        i++;
        if (i >= path.Length || path[i] is '.' or '[')
        {
            throw new ArgumentException(
                "Path contains an empty segment; segments must be separated by single dots, with no doubled or trailing dot.",
                nameof(path));
        }
    }

    /// <summary>Consumes a property-name segment, navigating into the object on success or
    /// returning a failed resolution.</summary>
    private static JsonPathResolution? ConsumeProperty(
        string path,
        ref int i,
        ref JsonElement current,
        StringBuilder prefix)
    {
        var propertyEnd = i;
        while (propertyEnd < path.Length && path[propertyEnd] is not '.' and not '[')
        {
            propertyEnd++;
        }

        var property = path[i..propertyEnd];
        if (current.ValueKind is not JsonValueKind.Object
            || !current.TryGetProperty(property, out var next))
        {
            return JsonPathResolution.NotFound(prefix.ToString(), property, current.ValueKind);
        }

        current = next;
        if (prefix.Length > 0)
        {
            prefix.Append('.');
        }

        prefix.Append(property);
        i = propertyEnd;
        return null;
    }

    /// <summary>Strips the optional leading <c>$.</c> or bare <c>$</c> JSONPath root reference,
    /// returning the substring whose path grammar is segment-then-optional-segments.</summary>
    private static string NormalizeRoot(string path)
    {
        if (path[0] is not '$')
        {
            return path;
        }

        if (path.Length is 1)
        {
            return string.Empty;
        }

        if (path[1] is '.')
        {
            return path[2..];
        }

        if (path[1] is '[')
        {
            return path[1..];
        }

        throw new ArgumentException(
            "Path begins with '$' but is not followed by '.', '[', or end-of-path; the JSONPath root reference must be '$', '$.', or '$['.",
            nameof(path));
    }

    /// <summary>Parses a <c>[N]</c> bracket index, advancing <paramref name="i"/> past the
    /// closing bracket on success.</summary>
    private static int ParseIndex(string path, ref int i)
    {
        var close = path.IndexOf(']', i + 1);
        if (close < 0)
        {
            throw new ArgumentException(
                "Path contains an unclosed '['; bracket indices must be closed.",
                nameof(path));
        }

        var inside = path[(i + 1)..close];
        if (inside.Length is 0)
        {
            throw new ArgumentException(
                "Path contains an empty bracket index '[]'; indices must be a non-negative integer.",
                nameof(path));
        }

        if (!int.TryParse(inside, NumberStyles.None, CultureInfo.InvariantCulture, out var index))
        {
            throw new ArgumentException(
                "Path contains a non-numeric or negative bracket index '[" + inside + "]'; indices must be a non-negative integer.",
                nameof(path));
        }

        i = close + 1;
        return index;
    }

    /// <summary>Returns the array element at <paramref name="index"/> from
    /// <paramref name="array"/>. <see cref="JsonElement"/> has no random-access indexer, so
    /// this delegates to <see cref="Enumerable.ElementAt{TSource}(System.Collections.Generic.IEnumerable{TSource}, int)"/>;
    /// callers must have already checked bounds.</summary>
    private static JsonElement GetArrayElement(JsonElement array, int index)
        => array.EnumerateArray().ElementAt(index);

    /// <summary>Reports whether <paramref name="path"/> contains a wildcard array segment
    /// <c>[*]</c>, which matches every element of an array rather than a single index. Wildcard
    /// paths are resolved with <see cref="ResolveAll"/>; <see cref="Resolve"/> rejects <c>[*]</c>
    /// as a malformed (non-numeric) index.</summary>
    /// <param name="path">The path to inspect.</param>
    /// <returns><see langword="true"/> if <paramref name="path"/> contains a <c>[*]</c> segment.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    public static bool ContainsWildcard(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return path.Contains("[*]", StringComparison.Ordinal);
    }

    /// <summary>
    /// Resolves <paramref name="path"/> against <paramref name="element"/>, expanding each
    /// <c>[*]</c> wildcard across every element of the array it targets, and returns one
    /// <see cref="JsonPathResolution"/> per expanded concrete path. A path with no wildcard yields
    /// exactly one resolution (identical to <see cref="Resolve"/>). A <c>[*]</c> over an empty
    /// array yields zero resolutions: a "for all" over an empty set, so an all-must-resolve check
    /// passes vacuously (pair with a non-empty-array assertion when emptiness must fail). A
    /// <c>[*]</c> applied to a non-array yields a single failed resolution describing the mismatch.
    /// Each failed resolution carries the concrete prefix (e.g. <c>[1]</c>) so a wildcard failure
    /// names which element failed.
    /// </summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A path as in <see cref="Resolve"/>, optionally containing one or more
    /// <c>[*]</c> wildcard segments.</param>
    /// <returns>The resolutions for every concrete path the wildcards expand to, in document order.</returns>
    /// <exception cref="ArgumentException"><paramref name="path"/> is invalid; see <see cref="Resolve"/>.</exception>
    public static IReadOnlyList<JsonPathResolution> ResolveAll(JsonElement element, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var normalized = NormalizeRoot(path);
        var results = new List<JsonPathResolution>();
        if (normalized.Length is 0)
        {
            results.Add(JsonPathResolution.Resolved(element, string.Empty));
            return results;
        }

        Expand(element, ParseSegments(normalized), 0, new StringBuilder(), results);
        return results;
    }

    /// <summary>Recursively walks <paramref name="segments"/> from <paramref name="index"/>,
    /// branching at each wildcard, accumulating one resolution per concrete leaf path.</summary>
    private static void Expand(
        JsonElement current,
        IReadOnlyList<PathSegment> segments,
        int index,
        StringBuilder prefix,
        List<JsonPathResolution> results)
    {
        if (index == segments.Count)
        {
            results.Add(JsonPathResolution.Resolved(current, prefix.ToString()));
            return;
        }

        var segment = segments[index];
        switch (segment.Kind)
        {
            case SegmentKind.Property:
                ExpandProperty(current, segment.Property, segments, index, prefix, results);
                break;
            case SegmentKind.Index:
                ExpandIndex(current, segment.Index, segments, index, prefix, results);
                break;
            default:
                ExpandWildcard(current, segments, index, prefix, results);
                break;
        }
    }

    private static void ExpandProperty(
        JsonElement current, string property, IReadOnlyList<PathSegment> segments, int index, StringBuilder prefix, List<JsonPathResolution> results)
    {
        if (current.ValueKind is not JsonValueKind.Object || !current.TryGetProperty(property, out var next))
        {
            results.Add(JsonPathResolution.NotFound(prefix.ToString(), property, current.ValueKind));
            return;
        }

        var saved = prefix.Length;
        if (prefix.Length > 0)
        {
            prefix.Append('.');
        }

        prefix.Append(property);
        Expand(next, segments, index + 1, prefix, results);
        prefix.Length = saved;
    }

    private static void ExpandIndex(
        JsonElement current, int arrayIndex, IReadOnlyList<PathSegment> segments, int index, StringBuilder prefix, List<JsonPathResolution> results)
    {
        var segmentText = string.Concat("[", arrayIndex.ToString(CultureInfo.InvariantCulture), "]");
        if (current.ValueKind is not JsonValueKind.Array || arrayIndex >= current.GetArrayLength())
        {
            results.Add(JsonPathResolution.NotFound(prefix.ToString(), segmentText, current.ValueKind));
            return;
        }

        var saved = prefix.Length;
        prefix.Append(segmentText);
        Expand(GetArrayElement(current, arrayIndex), segments, index + 1, prefix, results);
        prefix.Length = saved;
    }

    private static void ExpandWildcard(
        JsonElement current, IReadOnlyList<PathSegment> segments, int index, StringBuilder prefix, List<JsonPathResolution> results)
    {
        if (current.ValueKind is not JsonValueKind.Array)
        {
            results.Add(JsonPathResolution.NotFound(prefix.ToString(), "[*]", current.ValueKind));
            return;
        }

        var i = 0;
        foreach (var item in current.EnumerateArray())
        {
            var saved = prefix.Length;
            prefix.Append('[').Append(i.ToString(CultureInfo.InvariantCulture)).Append(']');
            Expand(item, segments, index + 1, prefix, results);
            prefix.Length = saved;
            i++;
        }
    }

    /// <summary>Tokenizes a root-normalized path into property / index / wildcard segments, applying
    /// the same grammar rules <see cref="WalkSegments"/> enforces (a property after <c>]</c> needs a
    /// dot; no leading / doubled / trailing dot; closed brackets), with <c>[*]</c> added.</summary>
    private static List<PathSegment> ParseSegments(string path)
    {
        var segments = new List<PathSegment>();
        var i = 0;
        while (i < path.Length)
        {
            var c = path[i];
            if (c is '[')
            {
                segments.Add(ParseBracketSegment(path, ref i));
            }
            else if (c is '.')
            {
                ConsumeDot(path, ref i, segments.Count);
            }
            else
            {
                segments.Add(ParsePropertySegment(path, ref i));
            }
        }

        return segments;
    }

    private static PathSegment ParseBracketSegment(string path, ref int i)
    {
        var close = path.IndexOf(']', i + 1);
        if (close < 0)
        {
            throw new ArgumentException(
                "Path contains an unclosed '['; bracket indices must be closed.", nameof(path));
        }

        var inside = path[(i + 1)..close];
        i = close + 1;
        if (i < path.Length && path[i] is not '.' and not '[')
        {
            throw new ArgumentException(
                "Path segment after ']' must be '.', '[', or end-of-path; a property name following an index requires a dot separator (use 'items[0].name', not 'items[0]name').",
                nameof(path));
        }

        if (string.Equals(inside, "*", StringComparison.Ordinal))
        {
            return new PathSegment(SegmentKind.Wildcard, string.Empty, 0);
        }

        if (inside.Length is 0)
        {
            throw new ArgumentException(
                "Path contains an empty bracket index '[]'; indices must be a non-negative integer or the '*' wildcard.", nameof(path));
        }

        if (!int.TryParse(inside, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new ArgumentException(
                "Path contains a non-numeric or negative bracket index '[" + inside + "]'; indices must be a non-negative integer or the '*' wildcard.",
                nameof(path));
        }

        return new PathSegment(SegmentKind.Index, string.Empty, parsed);
    }

    private static PathSegment ParsePropertySegment(string path, ref int i)
    {
        var end = i;
        while (end < path.Length && path[end] is not '.' and not '[')
        {
            end++;
        }

        var property = path[i..end];
        i = end;
        return new PathSegment(SegmentKind.Property, property, 0);
    }

    private enum SegmentKind
    {
        Property,
        Index,
        Wildcard,
    }

    private readonly record struct PathSegment(SegmentKind Kind, string Property, int Index);
}
