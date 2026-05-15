using System;
using System.Text.Json;

namespace JsonAssertions;

/// <summary>
/// Framework-agnostic value comparison for a resolved <see cref="JsonElement"/>. Each
/// <c>Matches</c> overload pairs a JSON value kind with a CLR expected value and reports
/// whether the element both is of the matching kind and carries the expected value. Kind
/// mismatches return <see langword="false"/> rather than throwing, so the caller can render a
/// "found a String, expected a Number" diagnostic instead of catching an exception.
/// </summary>
/// <remarks>
/// Named <c>JsonValueComparison</c> rather than <c>JsonValue</c> to avoid colliding with
/// <see cref="System.Text.Json.Nodes.JsonValue"/> for consumers who use the
/// <c>System.Text.Json.Nodes</c> object model alongside this package.
/// </remarks>
public static class JsonValueComparison
{
    /// <summary>Reports whether <paramref name="element"/> is a JSON string equal to
    /// <paramref name="expected"/> (ordinal comparison).</summary>
    public static bool Matches(JsonElement element, string expected)
        => element.ValueKind is JsonValueKind.String && element.ValueEquals(expected);

    /// <summary>Reports whether <paramref name="element"/> is a JSON boolean equal to
    /// <paramref name="expected"/>.</summary>
    public static bool Matches(JsonElement element, bool expected) => element.ValueKind switch
    {
        JsonValueKind.True => expected,
        JsonValueKind.False => !expected,
        _ => false,
    };

    /// <summary>Reports whether <paramref name="element"/> is a JSON number equal to
    /// <paramref name="expected"/>. The element is read as a <see cref="double"/>, so callers
    /// passing <see cref="int"/> or <see cref="long"/> literals are widened at the call site;
    /// values beyond <see cref="double"/> precision are out of scope for this overload.</summary>
    public static bool Matches(JsonElement element, double expected)
        => element.ValueKind is JsonValueKind.Number
            && element.TryGetDouble(out var actual)
            && actual.Equals(expected);

    /// <summary>Reports whether <paramref name="element"/> is a JSON string equal (ordinal)
    /// to any of <paramref name="candidates"/>. <see langword="false"/> when the element is
    /// not a JSON string, or when none of the candidates matches.</summary>
    public static bool MatchesAny(JsonElement element, params string[] candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        if (element.ValueKind is not JsonValueKind.String)
        {
            return false;
        }

        var captured = element;
        return Array.Exists(candidates, c => captured.ValueEquals(c));
    }

    /// <summary>Reports whether <paramref name="element"/> is a JSON number equal to any of
    /// <paramref name="candidates"/>. Element values beyond <see cref="double"/> precision are
    /// out of scope for this overload.</summary>
    public static bool MatchesAny(JsonElement element, params double[] candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        if (element.ValueKind is not JsonValueKind.Number || !element.TryGetDouble(out var actual))
        {
            return false;
        }

        var capturedActual = actual;
        return Array.Exists(candidates, c => c.Equals(capturedActual));
    }
}
