using System;
using System.Collections.Generic;

namespace JsonAssertions;

/// <summary>
/// Options for <see cref="JsonEquivalence.Compare(System.Text.Json.JsonElement, System.Text.Json.JsonElement, JsonEquivalenceOptions)"/>:
/// the JSON-path locations excluded from the comparison, and whether arrays are compared
/// order-insensitively. Configured through the fluent <see cref="IgnorePath"/> and
/// <see cref="IgnoreArrayOrder"/> methods inside the configure callback.
/// </summary>
public sealed class JsonEquivalenceOptions
{
    private readonly List<string> _ignoredPaths = [];

    /// <summary>The JSON paths excluded from the comparison, in registration order. Each is resolved
    /// (wildcards expanded) against both documents; a value at an ignored path contributes nothing to
    /// equivalence, whether it is present, absent, or differs. Returned as a read-only view; register
    /// paths through <see cref="IgnorePath"/> so the non-empty validation is enforced.</summary>
    public IReadOnlyList<string> IgnoredPaths => _ignoredPaths.AsReadOnly();

    /// <summary>Whether arrays are compared without regard to element order. Off by default: arrays
    /// are ordered in JSON, so equality is position-sensitive unless this is enabled. When enabled,
    /// two arrays are equivalent if they have the same length and each element on one side has a
    /// distinct equivalent element on the other (multiset equality).</summary>
    public bool IgnoreArrayOrderEnabled { get; private set; }

    /// <summary>Excludes a JSON path from the comparison. The path uses the same grammar as the
    /// assertion paths, including the <c>[*]</c> wildcard (so <c>items[*].timestamp</c> ignores that
    /// field on every array element). A path that does not resolve on either document is a no-op.
    /// Combining a positional array-index path with <see cref="IgnoreArrayOrder"/> is not meaningful,
    /// since order-insensitive matching makes indices ambiguous; use the <c>[*]</c> wildcard to ignore
    /// a field across all elements instead.</summary>
    /// <param name="path">The JSON path to ignore. Must be non-empty.</param>
    /// <returns>This instance, for chaining.</returns>
    /// <summary>
    /// Registers a JSON path to be excluded from equivalence comparisons.
    /// </summary>
    /// <param name="path">The JSON path to ignore during comparisons.</param>
    /// <returns>The current instance for fluent configuration chaining.</returns>
    /// <exception cref="ArgumentException"><paramref name="path"/> is null, empty, or whitespace.</exception>
    public JsonEquivalenceOptions IgnorePath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _ignoredPaths.Add(path);
        return this;
    }

    /// <summary>Compares arrays order-insensitively for the remainder of this comparison (see
    /// <see cref="IgnoreArrayOrderEnabled"/>).</summary>
    /// <summary>
    /// Configures the equivalence comparison to ignore the order of array elements.
    /// </summary>
    /// <returns>This instance, for method chaining.</returns>
    public JsonEquivalenceOptions IgnoreArrayOrder()
    {
        IgnoreArrayOrderEnabled = true;
        return this;
    }
}
