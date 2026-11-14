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
/// <item><c>objects[0].entries[0].id</c> &#x2014; mixed nesting.</item>
/// <item><c>items[*].id</c> &#x2014; the wildcard, which <see cref="ResolveAll"/> expands across every element of the
/// array; <see cref="Resolve"/> reads one location and rejects it.</item>
/// <item><c>$</c> &#x2014; the root element itself; <c>$.user.name</c> equivalent to
/// <c>user.name</c>; <c>$[0]</c> equivalent to <c>[0]</c> against a root array. Any other leading <c>$</c> starts a
/// name: <c>$type</c> is the property <c>$type</c>, as System.Text.Json writes it for polymorphic types.</item>
/// </list>
/// <para>A name runs up to the next <c>.</c> or <c>[</c>, so it cannot contain either, and the empty name cannot be
/// addressed. A malformed path throws <see cref="ArgumentException"/> before any navigation, naming the path and the
/// character where it goes wrong.</para>
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
    /// indices, for example <c>items[0].name</c>. A leading <c>$.</c>, <c>$[</c>, or a bare <c>$</c> is the JSONPath
    /// root reference; <c>$</c> alone resolves to <paramref name="element"/> itself.</param>
    /// <returns>A <see cref="JsonPathResolution"/> describing the outcome.</returns>
    /// <exception cref="ArgumentException"><paramref name="path"/> is <see langword="null"/>, empty or whitespace;
    /// is malformed (a leading, doubled or trailing dot, <c>$.</c> with nothing or an index after it, an empty, unclosed,
    /// negative, non-numeric or too-large index, or a name directly after <c>]</c>); or contains the <c>[*]</c>
    /// wildcard, which <see cref="ResolveAll"/> expands. The path is checked in full before any navigation. Also thrown
    /// when <paramref name="element"/> is <see langword="default"/>, which holds no JSON value (the path is checked
    /// first).</exception>
    public static JsonPathResolution Resolve(JsonElement element, string path)
    {
        var segments = Parse(path);
        RequireValue(element);
        if (segments.Exists(static segment => segment.Kind is SegmentKind.Wildcard))
        {
            throw new ArgumentException(
                "Path '" + path + "' contains the [*] wildcard, which matches every element of an array, but this reads one location; "
                + "resolve it with JsonPath.ResolveAll, assert it with HasJsonProperty, HasJsonProperties or HasJsonValueMatching, or read it with GetJsonValues.",
                nameof(path));
        }

        var current = element;
        var prefix = new StringBuilder();
        foreach (var segment in segments)
        {
            var failure = segment.Kind is SegmentKind.Property
                ? StepIntoProperty(ref current, segment.Property, prefix)
                : StepIntoIndex(ref current, segment.Index, prefix);
            if (failure is not null)
            {
                return failure.Value;
            }
        }

        return JsonPathResolution.Resolved(current, prefix.ToString());
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
    /// <exception cref="ArgumentException"><paramref name="path"/> is invalid, or <paramref name="element"/> is
    /// <see langword="default"/>; see <see cref="Resolve"/>.</exception>
    public static bool Exists(JsonElement element, string path) => Resolve(element, path).Found;

    /// <summary>Reports whether <paramref name="path"/> contains a wildcard array segment
    /// <c>[*]</c>, which matches every element of an array rather than a single index. Wildcard
    /// paths are resolved with <see cref="ResolveAll"/>; <see cref="Resolve"/> rejects them.</summary>
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
    /// <exception cref="ArgumentException"><paramref name="path"/> is malformed; see <see cref="Resolve"/>. The path is
    /// checked in full before any navigation. Also thrown when <paramref name="element"/> is <see langword="default"/>,
    /// which holds no JSON value.</exception>
    public static IReadOnlyList<JsonPathResolution> ResolveAll(JsonElement element, string path)
    {
        var segments = Parse(path);
        RequireValue(element);
        var results = new List<JsonPathResolution>();
        Expand(element, segments, 0, new StringBuilder(), results);
        return results;
    }

    /// <summary>Throws when <paramref name="element"/> is <see langword="default"/>: it holds no JSON value, so even
    /// <c>$</c> has nothing to resolve to.</summary>
    private static void RequireValue(JsonElement element)
    {
        if (element.ValueKind is JsonValueKind.Undefined)
        {
            throw new ArgumentException("The JsonElement is default: it holds no JSON value.", nameof(element));
        }
    }

    /// <summary>Checks <paramref name="path"/> against the grammar without navigating, for options that register a
    /// path to use later (<c>IgnorePath</c>, <c>ScrubPath</c>); the <c>[*]</c> wildcard is allowed.</summary>
    /// <param name="path">The path to check.</param>
    /// <exception cref="ArgumentException"><paramref name="path"/> is malformed; see <see cref="Resolve"/>.</exception>
    internal static void Validate(string path) => _ = Parse(path);

    /// <summary>Checks each of <paramref name="paths"/> against the grammar without navigating, for assertions that
    /// take several paths in a parameter named <c>paths</c>. An error names the element by its index, such as
    /// <c>paths[1]</c>; the <c>[*]</c> wildcard is allowed.</summary>
    /// <param name="paths">The paths to check.</param>
    /// <exception cref="ArgumentNullException"><paramref name="paths"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="paths"/> is empty, or one of them is
    /// <see langword="null"/>, blank, malformed or a repeat of an earlier one.</exception>
    internal static void ValidateAll(string[] paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        if (paths.Length is 0)
        {
            throw new ArgumentException(
                "No paths were given; pass at least one (for one path, HasJsonProperty reads the same).", nameof(paths));
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < paths.Length; i++)
        {
            var element = nameof(paths) + "[" + i.ToString(CultureInfo.InvariantCulture) + "]";
            if (string.IsNullOrWhiteSpace(paths[i]))
            {
                throw new ArgumentException("The path at " + element + " is null, empty or white space.", nameof(paths));
            }

            var path = new PathText(paths[i], nameof(paths), " (" + element + ")");
            if (!seen.Add(Canonical(Parse(path))))
            {
                throw path.Malformed("repeats an earlier path; give each path once.");
            }
        }
    }

    /// <summary>The failed resolution of an index into <paramref name="current"/>, which is not an array or is too
    /// short; for an array it carries the length.</summary>
    private static JsonPathResolution IndexNotFound(JsonElement current, string segmentText, StringBuilder prefix)
        => JsonPathResolution.NotFound(
            prefix.ToString(),
            segmentText,
            current.ValueKind,
            current.ValueKind is JsonValueKind.Array ? current.GetArrayLength() : 0);

    /// <summary>Steps from <paramref name="current"/> into its property <paramref name="property"/>, or returns the
    /// failed resolution when <paramref name="current"/> is not an object or has no such property.</summary>
    private static JsonPathResolution? StepIntoProperty(ref JsonElement current, string property, StringBuilder prefix)
    {
        if (current.ValueKind is not JsonValueKind.Object || !current.TryGetProperty(property, out var next))
        {
            return JsonPathResolution.NotFound(prefix.ToString(), property, current.ValueKind);
        }

        current = next;
        AppendProperty(prefix, property);
        return null;
    }

    /// <summary>Steps from <paramref name="current"/> into its element <paramref name="index"/>, or returns the failed
    /// resolution when <paramref name="current"/> is not an array or the index is out of range.</summary>
    private static JsonPathResolution? StepIntoIndex(ref JsonElement current, int index, StringBuilder prefix)
    {
        var segmentText = IndexText(index);
        if (current.ValueKind is not JsonValueKind.Array || index >= current.GetArrayLength())
        {
            return IndexNotFound(current, segmentText, prefix);
        }

        current = current[index];
        prefix.Append(segmentText);
        return null;
    }

    private static void AppendProperty(StringBuilder prefix, string property)
    {
        if (prefix.Length > 0)
        {
            prefix.Append('.');
        }

        prefix.Append(property);
    }

    private static string IndexText(int index) => string.Concat("[", index.ToString(CultureInfo.InvariantCulture), "]");

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
        AppendProperty(prefix, property);
        Expand(next, segments, index + 1, prefix, results);
        prefix.Length = saved;
    }

    private static void ExpandIndex(
        JsonElement current, int arrayIndex, IReadOnlyList<PathSegment> segments, int index, StringBuilder prefix, List<JsonPathResolution> results)
    {
        var segmentText = IndexText(arrayIndex);
        if (current.ValueKind is not JsonValueKind.Array || arrayIndex >= current.GetArrayLength())
        {
            results.Add(IndexNotFound(current, segmentText, prefix));
            return;
        }

        var saved = prefix.Length;
        prefix.Append(segmentText);
        Expand(current[arrayIndex], segments, index + 1, prefix, results);
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
            prefix.Append(IndexText(i));
            Expand(item, segments, index + 1, prefix, results);
            prefix.Length = saved;
            i++;
        }
    }

    /// <summary>Tokenizes <paramref name="path"/> into property, index and wildcard segments, checking the whole
    /// grammar before any navigation: the root reference, single dots between segments, closed brackets with a
    /// non-negative integer or <c>*</c>, and a dot before a name that follows <c>]</c>.</summary>
    private static List<PathSegment> Parse(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Parse(new PathText(path, nameof(path), string.Empty));
    }

    /// <summary>Tokenizes a path that is neither <see langword="null"/> nor blank.</summary>
    private static List<PathSegment> Parse(PathText path)
    {
        var text = path.Value;
        var segments = new List<PathSegment>();
        var i = RootLength(path);
        while (i < text.Length)
        {
            var c = text[i];
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
                segments.Add(ParsePropertySegment(text, ref i));
            }
        }

        return segments;
    }

    /// <summary>The length of the root reference at the start of <paramref name="path"/>: 1 for <c>$</c> alone and
    /// <c>$[</c>, 2 for <c>$.</c>, and 0 otherwise (a <c>$</c> followed by anything else starts a name).</summary>
    private static int RootLength(PathText path) => path.Value switch
    {
        ['$'] or ['$', '[', ..] => 1,
        ['$', '.', ..] => RootDotLength(path),
        _ => 0,
    };

    /// <summary>The length of a <c>$.</c> root reference, which must be followed by a name: <c>$.</c>, <c>$..a</c> and
    /// <c>$.[0]</c> have an empty segment after the root.</summary>
    private static int RootDotLength(PathText path)
        => path.Value.Length is 2 || path.Value[2] is '.' or '[' ? throw EmptySegment(path, 2) : 2;

    /// <summary>Consumes a single <c>.</c> separator, which must sit between two segments.</summary>
    private static void ConsumeDot(PathText path, ref int i, int segmentCount)
    {
        if (segmentCount is 0)
        {
            throw path.Malformed("begins with '.'; a path starts with a name, an index such as [0], or '$'.");
        }

        i++;
        if (i >= path.Value.Length || path.Value[i] is '.' or '[')
        {
            throw EmptySegment(path, i);
        }
    }

    /// <summary>The error for an empty segment at <paramref name="position"/> (a doubled or trailing dot, or a dot
    /// directly before an index).</summary>
    private static ArgumentException EmptySegment(PathText path, int position)
    {
        var descent = path.Value.Contains("..", StringComparison.Ordinal)
            ? " ('..' is JSONPath's recursive descent, which is not supported)"
            : string.Empty;
        return path.Malformed(
            "has an empty segment at character " + Character(position)
            + "; segments are separated by single dots, with no doubled or trailing dot" + descent + ".");
    }

    private static PathSegment ParseBracketSegment(PathText path, ref int i)
    {
        var text = path.Value;
        var open = i;
        var closeOffset = text.AsSpan(i + 1).IndexOf(']');
        var close = closeOffset < 0 ? closeOffset : i + 1 + closeOffset;
        if (close < 0)
        {
            throw path.Malformed("has an unclosed '[' at character " + Character(open) + ".");
        }

        var inside = text[(i + 1)..close];
        i = close + 1;
        if (i < text.Length && text[i] is not '.' and not '[')
        {
            throw path.Malformed(
                "has a name directly after ']' at character " + Character(i)
                + "; a name after an index needs a dot (items[0].name, not items[0]name).");
        }

        if (string.Equals(inside, "*", StringComparison.Ordinal))
        {
            return new PathSegment(SegmentKind.Wildcard, string.Empty, 0);
        }

        return new PathSegment(SegmentKind.Index, string.Empty, ParseIndex(path, inside, open));
    }

    /// <summary>Parses the text of a bracket index, rejecting an empty, negative, non-numeric or too-large index.</summary>
    private static int ParseIndex(PathText path, string inside, int open)
    {
        if (int.TryParse(inside, NumberStyles.None, CultureInfo.InvariantCulture, out var index))
        {
            return index;
        }

        string problem;
        if (inside.Length is 0)
        {
            problem = "an empty index";
        }
        else if (inside.All(char.IsAsciiDigit))
        {
            problem = "the index '[" + inside + "]', which is too large (the maximum is 2147483647)";
        }
        else
        {
            problem = "the index '[" + inside + "]', which is not a non-negative integer or the [*] wildcard";
        }

        throw path.Malformed("has " + problem + " at character " + Character(open) + ".");
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

    /// <summary>One spelling per location, so two paths that name the same location give the same text: <c>a</c> and
    /// <c>$.a</c>, or <c>items[1]</c> and <c>items[01]</c>.</summary>
    private static string Canonical(List<PathSegment> segments)
    {
        var sb = new StringBuilder();
        foreach (var segment in segments)
        {
            _ = segment.Kind switch
            {
                SegmentKind.Property => sb.Append('.').Append(segment.Property),
                SegmentKind.Index => sb.Append(IndexText(segment.Index)),
                _ => sb.Append("[*]"),
            };
        }

        return sb.ToString();
    }

    /// <summary>A 1-based character position, for messages.</summary>
    private static string Character(int index) => (index + 1).ToString(CultureInfo.InvariantCulture);

    private enum SegmentKind
    {
        Property,
        Index,
        Wildcard,
    }

    private readonly record struct PathSegment(SegmentKind Kind, string Property, int Index);

    /// <summary>A path being tokenized, with what its errors name: the parameter, and where in that parameter the
    /// path sits (empty for a single path, <c> (paths[1])</c> for an element of a path array).</summary>
    /// <param name="Value">The path text.</param>
    /// <param name="ParamName">The parameter the path came from.</param>
    /// <param name="Where">The position within that parameter, as rendered after the quoted path.</param>
    private readonly record struct PathText(string Value, string ParamName, string Where)
    {
        /// <summary>The error for this path, with <paramref name="problem"/> after the quoted path.</summary>
        public ArgumentException Malformed(string problem)
            => new("Path '" + Value + "'" + Where + " " + problem, ParamName);
    }
}
