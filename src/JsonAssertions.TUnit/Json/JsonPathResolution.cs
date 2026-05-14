using System.Text.Json;

namespace JsonAssertions;

/// <summary>
/// The outcome of resolving a dot-separated JSON path against a <see cref="JsonElement"/>.
/// Carries enough context for an assertion to render a failure message that says <em>where</em>
/// on the path resolution stopped, not merely that it did, the diagnostics that justify this
/// being a package rather than a hand-rolled <c>TryGetProperty(...).IsTrue()</c> helper.
/// </summary>
public readonly record struct JsonPathResolution
{
    private JsonPathResolution(
        bool found,
        JsonElement element,
        string resolvedPrefix,
        string? failedSegment,
        JsonValueKind containerKind)
    {
        Found = found;
        Element = element;
        ResolvedPrefix = resolvedPrefix;
        FailedSegment = failedSegment;
        ContainerKind = containerKind;
    }

    /// <summary>Whether the full path resolved to an existing property.</summary>
    public bool Found { get; }

    /// <summary>The resolved element. Meaningful only when <see cref="Found"/> is
    /// <see langword="true"/>; otherwise <see langword="default"/>.</summary>
    public JsonElement Element { get; }

    /// <summary>The longest path prefix that resolved before the failure point, for example
    /// <c>user.address</c> when resolving <c>user.address.city</c> stopped at <c>city</c>. An
    /// empty string when the very first segment failed against the root element. The full path
    /// when <see cref="Found"/> is <see langword="true"/>.</summary>
    public string ResolvedPrefix { get; }

    /// <summary>The segment that could not be resolved, or <see langword="null"/> when
    /// <see cref="Found"/> is <see langword="true"/>.</summary>
    public string? FailedSegment { get; }

    /// <summary>The <see cref="JsonValueKind"/> of the element that <see cref="FailedSegment"/>
    /// was looked up on (the element at <see cref="ResolvedPrefix"/>). Distinguishes "the
    /// object has no such property" (<see cref="JsonValueKind.Object"/>) from "the path tried
    /// to read a property off a non-object" (<see cref="JsonValueKind.String"/>,
    /// <see cref="JsonValueKind.Array"/>, etc.). Meaningful only when <see cref="Found"/> is
    /// <see langword="false"/>.</summary>
    public JsonValueKind ContainerKind { get; }

    /// <summary>Creates a successful resolution carrying the resolved <paramref name="element"/>.</summary>
    internal static JsonPathResolution Resolved(JsonElement element, string path)
        => new(found: true, element, path, failedSegment: null, element.ValueKind);

    /// <summary>Creates a failed resolution carrying the failure-point context.</summary>
    internal static JsonPathResolution NotFound(
        string resolvedPrefix,
        string failedSegment,
        JsonValueKind containerKind)
        => new(found: false, default, resolvedPrefix, failedSegment, containerKind);
}
