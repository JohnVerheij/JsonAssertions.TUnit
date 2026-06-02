using System;
using System.Globalization;
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
    /// <paramref name="expected"/>. The element is read as a <see cref="double"/>; values beyond
    /// <see cref="double"/> precision are out of scope for this overload (use the <see cref="long"/>
    /// / <see cref="ulong"/> overloads for exact 64-bit integers, which also accept the
    /// string-encoded form).</summary>
    public static bool Matches(JsonElement element, double expected)
        => element.ValueKind is JsonValueKind.Number
            && element.TryGetDouble(out var actual)
            && actual.Equals(expected);

    /// <summary>Reports whether <paramref name="element"/> is a JSON integer equal to
    /// <paramref name="expected"/>, encoded either as a JSON <em>number</em> (read as an exact
    /// 32-bit integer via <see cref="JsonElement.TryGetInt32(out int)"/>) or as a numeric JSON
    /// <em>string</em> that parses to a <see cref="int"/> against
    /// <see cref="CultureInfo.InvariantCulture"/>. A number that is fractional or out of
    /// <see cref="int"/> range, a non-numeric string, or any other kind returns
    /// <see langword="false"/>. Both encodings are accepted because the JSON form of an integer
    /// depends on the serializer: System.Text.Json writes a 32-bit integer as a JSON number, while
    /// Protobuf's <c>JsonFormatter</c> can emit it as a JSON string.</summary>
    public static bool Matches(JsonElement element, int expected) => element.ValueKind switch
    {
        JsonValueKind.Number => element.TryGetInt32(out var actual) && actual == expected,
        JsonValueKind.String => element.GetString() is { } raw
            && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var actual)
            && actual == expected,
        _ => false,
    };

    /// <summary>Reports whether <paramref name="element"/> is a JSON unsigned integer equal to
    /// <paramref name="expected"/>, encoded either as a JSON <em>number</em> (read as an exact
    /// unsigned 32-bit integer via <see cref="JsonElement.TryGetUInt32(out uint)"/>) or as a numeric
    /// JSON <em>string</em> that parses to a <see cref="uint"/> against
    /// <see cref="CultureInfo.InvariantCulture"/>. A number that is fractional, negative, or out of
    /// <see cref="uint"/> range, a non-numeric string, or any other kind returns
    /// <see langword="false"/>.</summary>
    public static bool Matches(JsonElement element, uint expected) => element.ValueKind switch
    {
        JsonValueKind.Number => element.TryGetUInt32(out var actual) && actual == expected,
        JsonValueKind.String => element.GetString() is { } raw
            && uint.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var actual)
            && actual == expected,
        _ => false,
    };

    /// <summary>Reports whether <paramref name="element"/> is a JSON 64-bit integer equal to
    /// <paramref name="expected"/>, encoded either as a JSON <em>number</em> (read as an exact
    /// 64-bit integer via <see cref="JsonElement.TryGetInt64(out long)"/>) or as a numeric JSON
    /// <em>string</em> that parses to a <see cref="long"/> against
    /// <see cref="CultureInfo.InvariantCulture"/>. A number that is fractional or out of
    /// <see cref="long"/> range, a non-numeric string, or any other kind returns
    /// <see langword="false"/>. The 64-bit overload exists because Protobuf's <c>JsonFormatter</c>
    /// serializes <c>int64</c> as a JSON string to avoid 53-bit precision loss, while
    /// System.Text.Json writes it as a JSON number; both encodings are matched.</summary>
    public static bool Matches(JsonElement element, long expected) => element.ValueKind switch
    {
        JsonValueKind.Number => element.TryGetInt64(out var actual) && actual == expected,
        JsonValueKind.String => element.GetString() is { } raw
            && long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var actual)
            && actual == expected,
        _ => false,
    };

    /// <summary>Reports whether <paramref name="element"/> is a JSON unsigned 64-bit integer equal
    /// to <paramref name="expected"/>, encoded either as a JSON <em>number</em> (read as an exact
    /// unsigned 64-bit integer via <see cref="JsonElement.TryGetUInt64(out ulong)"/>) or as a
    /// numeric JSON <em>string</em> that parses to a <see cref="ulong"/> against
    /// <see cref="CultureInfo.InvariantCulture"/>. A number that is fractional, negative, or out of
    /// <see cref="ulong"/> range, a non-numeric string, or any other kind returns
    /// <see langword="false"/>. Mirrors the <see cref="long"/> overload for unsigned values
    /// (Protobuf's <c>JsonFormatter</c> serializes <c>uint64</c> as a JSON string).</summary>
    public static bool Matches(JsonElement element, ulong expected) => element.ValueKind switch
    {
        JsonValueKind.Number => element.TryGetUInt64(out var actual) && actual == expected,
        JsonValueKind.String => element.GetString() is { } raw
            && ulong.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var actual)
            && actual == expected,
        _ => false,
    };

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

    /// <summary>Reports whether <paramref name="element"/> is a JSON 32-bit integer equal to any of
    /// <paramref name="candidates"/>, encoded either as a JSON <em>number</em> (read via
    /// <see cref="JsonElement.TryGetInt32(out int)"/>) or as a numeric JSON <em>string</em> that
    /// parses to a <see cref="int"/> against <see cref="CultureInfo.InvariantCulture"/>. A
    /// fractional or out-of-range number, a non-numeric string, or any other kind returns
    /// <see langword="false"/>.</summary>
    public static bool MatchesAny(JsonElement element, params int[] candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        if (!TryReadInt32(element, out var actual))
        {
            return false;
        }

        var capturedActual = actual;
        return Array.Exists(candidates, c => c == capturedActual);
    }

    /// <summary>Reports whether <paramref name="element"/> is a JSON unsigned 32-bit integer equal
    /// to any of <paramref name="candidates"/>, encoded either as a JSON <em>number</em> (read via
    /// <see cref="JsonElement.TryGetUInt32(out uint)"/>) or as a numeric JSON <em>string</em> that
    /// parses to a <see cref="uint"/> against <see cref="CultureInfo.InvariantCulture"/>. A
    /// fractional, negative, or out-of-range number, a non-numeric string, or any other kind
    /// returns <see langword="false"/>.</summary>
    public static bool MatchesAny(JsonElement element, params uint[] candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        if (!TryReadUInt32(element, out var actual))
        {
            return false;
        }

        var capturedActual = actual;
        return Array.Exists(candidates, c => c == capturedActual);
    }

    /// <summary>Reports whether <paramref name="element"/> is a JSON 64-bit integer equal to any of
    /// <paramref name="candidates"/>, encoded either as a JSON <em>number</em> (read via
    /// <see cref="JsonElement.TryGetInt64(out long)"/>) or as a numeric JSON <em>string</em> that
    /// parses to a <see cref="long"/> against <see cref="CultureInfo.InvariantCulture"/>. A
    /// fractional or out-of-range number, a non-numeric string, or any other kind returns
    /// <see langword="false"/>.</summary>
    public static bool MatchesAny(JsonElement element, params long[] candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        if (!TryReadInt64(element, out var actual))
        {
            return false;
        }

        var capturedActual = actual;
        return Array.Exists(candidates, c => c == capturedActual);
    }

    /// <summary>Reports whether <paramref name="element"/> is a JSON unsigned 64-bit integer equal
    /// to any of <paramref name="candidates"/>, encoded either as a JSON <em>number</em> (read via
    /// <see cref="JsonElement.TryGetUInt64(out ulong)"/>) or as a numeric JSON <em>string</em> that
    /// parses to a <see cref="ulong"/> against <see cref="CultureInfo.InvariantCulture"/>. A
    /// fractional, negative, or out-of-range number, a non-numeric string, or any other kind
    /// returns <see langword="false"/>.</summary>
    public static bool MatchesAny(JsonElement element, params ulong[] candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        if (!TryReadUInt64(element, out var actual))
        {
            return false;
        }

        var capturedActual = actual;
        return Array.Exists(candidates, c => c == capturedActual);
    }

    /// <summary>Reads <paramref name="element"/> as a 32-bit integer from either a JSON number or a
    /// numeric JSON string parsed against <see cref="CultureInfo.InvariantCulture"/>.</summary>
    private static bool TryReadInt32(JsonElement element, out int value)
    {
        value = 0;
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetInt32(out value),
            JsonValueKind.String => int.TryParse(element.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value),
            _ => false,
        };
    }

    /// <summary>Reads <paramref name="element"/> as an unsigned 32-bit integer from either a JSON
    /// number or a numeric JSON string parsed against
    /// <see cref="CultureInfo.InvariantCulture"/>.</summary>
    private static bool TryReadUInt32(JsonElement element, out uint value)
    {
        value = 0;
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetUInt32(out value),
            JsonValueKind.String => uint.TryParse(element.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value),
            _ => false,
        };
    }

    /// <summary>Reads <paramref name="element"/> as a 64-bit integer from either a JSON number or a
    /// numeric JSON string parsed against <see cref="CultureInfo.InvariantCulture"/>.</summary>
    private static bool TryReadInt64(JsonElement element, out long value)
    {
        value = 0;
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetInt64(out value),
            JsonValueKind.String => long.TryParse(element.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value),
            _ => false,
        };
    }

    /// <summary>Reads <paramref name="element"/> as an unsigned 64-bit integer from either a JSON
    /// number or a numeric JSON string parsed against
    /// <see cref="CultureInfo.InvariantCulture"/>.</summary>
    private static bool TryReadUInt64(JsonElement element, out ulong value)
    {
        value = 0;
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetUInt64(out value),
            JsonValueKind.String => ulong.TryParse(element.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value),
            _ => false,
        };
    }
}
