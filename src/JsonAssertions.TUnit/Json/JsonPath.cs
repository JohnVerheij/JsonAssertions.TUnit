using System;
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
public static class JsonPath
{
    /// <summary>
    /// Resolves a dot-separated <paramref name="path"/> against <paramref name="element"/>,
    /// returning a <see cref="JsonPathResolution"/> that carries the resolved element on
    /// success and the failure-point context (how far it got, which segment failed, what kind
    /// of value blocked it) on failure.
    /// </summary>
    /// <param name="element">The JSON element to navigate from, typically a
    /// <see cref="JsonValueKind.Object"/>.</param>
    /// <param name="path">A dot-separated property path, for example
    /// <c>user.address.city</c>. A leading <c>$.</c> JSONPath-style root prefix is accepted
    /// and ignored, so <c>$.user.name</c> and <c>user.name</c> resolve identically.</param>
    /// <returns>A <see cref="JsonPathResolution"/> describing the outcome.</returns>
    /// <exception cref="ArgumentException"><paramref name="path"/> is <see langword="null"/>,
    /// empty, whitespace, or contains an empty segment (a leading, trailing, or doubled dot).
    /// </exception>
    public static JsonPathResolution Resolve(JsonElement element, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var normalized = path.StartsWith("$.", StringComparison.Ordinal) ? path[2..] : path;
        var current = element;
        var resolvedPrefix = string.Empty;

        foreach (var segment in normalized.Split('.'))
        {
            if (segment.Length is 0)
            {
                throw new ArgumentException(
                    "Path contains an empty segment; segments must be separated by single dots, with no leading or trailing dot.",
                    nameof(path));
            }

            if (current.ValueKind is not JsonValueKind.Object
                || !current.TryGetProperty(segment, out var next))
            {
                return JsonPathResolution.NotFound(resolvedPrefix, segment, current.ValueKind);
            }

            current = next;
            resolvedPrefix = resolvedPrefix.Length is 0 ? segment : resolvedPrefix + "." + segment;
        }

        return JsonPathResolution.Resolved(current, normalized);
    }

    /// <summary>
    /// Reports whether a property exists at the given dot-separated <paramref name="path"/>
    /// within <paramref name="element"/>. Equivalent to <c>Resolve(element, path).Found</c>.
    /// </summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A dot-separated property path; see <see cref="Resolve"/>.</param>
    /// <returns><see langword="true"/> if every segment resolves to an existing property;
    /// <see langword="false"/> if any segment is missing or the path traverses a value that is
    /// not a JSON object.</returns>
    /// <exception cref="ArgumentException"><paramref name="path"/> is invalid; see
    /// <see cref="Resolve"/>.</exception>
    public static bool Exists(JsonElement element, string path) => Resolve(element, path).Found;
}
