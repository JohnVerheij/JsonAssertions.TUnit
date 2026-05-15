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

    /// <summary>Reports whether <paramref name="element"/> is a JSON string with a non-zero
    /// length. A non-string kind returns <see langword="false"/>; an empty JSON string
    /// (<c>""</c>) returns <see langword="false"/>.</summary>
    public static bool IsNonEmptyString(JsonElement element)
        => element.ValueKind is JsonValueKind.String && (element.GetString()?.Length ?? 0) > 0;

    /// <summary>Reports whether <paramref name="element"/> is a JSON boolean of either value.
    /// JSON's <see langword="true"/> and <see langword="false"/> are distinct
    /// <see cref="JsonValueKind"/>s, so a "this field is a bool, either value" assertion
    /// cannot be expressed via <see cref="IsKind"/> alone.</summary>
    public static bool IsBoolean(JsonElement element)
        => element.ValueKind is JsonValueKind.True or JsonValueKind.False;
}
