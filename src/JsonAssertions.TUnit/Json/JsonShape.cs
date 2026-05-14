using System.Text.Json;

namespace JsonAssertions;

/// <summary>
/// Framework-agnostic shape predicates for a resolved <see cref="JsonElement"/>: its value
/// kind, and, for arrays, its length. Like <see cref="JsonValueComparison"/>, the predicates
/// return <see langword="false"/> on a kind mismatch rather than throwing, so a caller can
/// render a "found a String, expected an array" diagnostic instead of catching an exception.
/// </summary>
public static class JsonShape
{
    /// <summary>Reports whether <paramref name="element"/> is of the given
    /// <paramref name="kind"/>.</summary>
    public static bool IsKind(JsonElement element, JsonValueKind kind) => element.ValueKind == kind;

    /// <summary>Reports whether <paramref name="element"/> is a JSON array with exactly
    /// <paramref name="length"/> elements.</summary>
    public static bool IsArrayOfLength(JsonElement element, int length)
        => element.ValueKind is JsonValueKind.Array && element.GetArrayLength() == length;

    /// <summary>Reports whether <paramref name="element"/> is a JSON array with at least one
    /// element.</summary>
    public static bool IsNonEmptyArray(JsonElement element)
        => element.ValueKind is JsonValueKind.Array && element.GetArrayLength() > 0;

    /// <summary>Reports whether <paramref name="element"/> is a JSON array with no
    /// elements.</summary>
    public static bool IsEmptyArray(JsonElement element)
        => element.ValueKind is JsonValueKind.Array && element.GetArrayLength() is 0;
}
