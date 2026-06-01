using System;
using System.Collections.Generic;

namespace JsonAssertions;

/// <summary>
/// Options for <see cref="JsonCanonicalizer.Canonicalize(string, Action{JsonCanonicalizeOptions})"/>:
/// the JSON-path locations whose values are replaced with a stable token before the document is
/// re-serialized, and the token text itself. Configured through the fluent <see cref="ScrubPath"/>
/// and <see cref="WithScrubToken"/> methods inside the configure callback.
/// </summary>
public sealed class JsonCanonicalizeOptions
{
    private readonly List<string> _scrubPaths = [];

    /// <summary>The JSON paths registered for scrubbing, in registration order. Each is resolved
    /// (wildcards expanded) against the document; every value it targets is replaced with
    /// <see cref="ScrubToken"/>.</summary>
    public IReadOnlyList<string> ScrubPaths => _scrubPaths;

    /// <summary>The token a scrubbed value is replaced with. Defaults to <c>"&lt;scrubbed&gt;"</c>.</summary>
    public string ScrubToken { get; private set; } = "<scrubbed>";

    /// <summary>Registers a JSON path whose value is replaced with <see cref="ScrubToken"/> in the
    /// canonical output. The path uses the same grammar as the assertion paths, including the
    /// <c>[*]</c> wildcard (so <c>[*].eventBus.connectionInfo</c> scrubs that field on every array
    /// element). A path that does not resolve is a no-op.</summary>
    /// <param name="path">The JSON path to scrub. Must be non-empty.</param>
    /// <returns>This instance, for chaining.</returns>
    /// <exception cref="ArgumentException"><paramref name="path"/> is null, empty, or whitespace.</exception>
    public JsonCanonicalizeOptions ScrubPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _scrubPaths.Add(path);
        return this;
    }

    /// <summary>Overrides the token a scrubbed value is replaced with (default <c>"&lt;scrubbed&gt;"</c>).</summary>
    /// <param name="token">The replacement token.</param>
    /// <returns>This instance, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="token"/> is <see langword="null"/>.</exception>
    public JsonCanonicalizeOptions WithScrubToken(string token)
    {
        ArgumentNullException.ThrowIfNull(token);
        ScrubToken = token;
        return this;
    }
}
