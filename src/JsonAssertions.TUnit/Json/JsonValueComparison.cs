using System;
using System.Diagnostics.CodeAnalysis;
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
    /// string-encoded form). A literal beyond <see cref="double"/> range, such as <c>1e400</c>, reads as an
    /// infinity and never matches: JSON has no infinity.</summary>
    [SuppressMessage(
        "Major Bug",
        "S1244:Do not check floating point equality with exact values",
        Justification = "Exact equality is the documented contract of this overload: the caller supplies the value to match. A tolerance would pass values the caller did not ask for, with no way to opt out.")]
    public static bool Matches(JsonElement element, double expected)
        => TryReadDouble(element, out var actual) && actual.Equals(expected);

    /// <summary>Reports whether <paramref name="element"/> is a JSON integer equal to
    /// <paramref name="expected"/>, encoded either as a JSON <em>number</em> (read as an exact
    /// 32-bit integer via <see cref="JsonElement.TryGetInt32(out int)"/>) or as a JSON
    /// <em>string</em> that holds a JSON integer literal (an optional <c>-</c>, then <c>0</c> or digits that do
    /// not start with <c>0</c>) in <see cref="int"/> range. A number that is fractional or out of
    /// <see cref="int"/> range, any other string (with a <c>+</c>, a leading zero or white space), or any other kind
    /// returns <see langword="false"/>. Both encodings are accepted because the JSON form of an integer
    /// depends on the serializer: System.Text.Json writes a 32-bit integer as a JSON number, while
    /// Protobuf's <c>JsonFormatter</c> can emit it as a JSON string.</summary>
    public static bool Matches(JsonElement element, int expected)
        => TryReadInt32(element, out var actual) && actual == expected;

    /// <summary>Reports whether <paramref name="element"/> is a JSON unsigned integer equal to
    /// <paramref name="expected"/>, encoded either as a JSON <em>number</em> (read as an exact
    /// unsigned 32-bit integer via <see cref="JsonElement.TryGetUInt32(out uint)"/>; <c>-0</c> reads as zero) or
    /// as a JSON <em>string</em> that holds a JSON integer literal in <see cref="uint"/> range. A number that is
    /// fractional, negative, or out of <see cref="uint"/> range, any other string, or any other kind returns
    /// <see langword="false"/>.</summary>
    public static bool Matches(JsonElement element, uint expected)
        => TryReadUInt32(element, out var actual) && actual == expected;

    /// <summary>Reports whether <paramref name="element"/> is a JSON 64-bit integer equal to
    /// <paramref name="expected"/>, encoded either as a JSON <em>number</em> (read as an exact
    /// 64-bit integer via <see cref="JsonElement.TryGetInt64(out long)"/>) or as a JSON
    /// <em>string</em> that holds a JSON integer literal in <see cref="long"/> range. A number that is fractional
    /// or out of <see cref="long"/> range, any other string, or any other kind returns
    /// <see langword="false"/>. The 64-bit overload exists because Protobuf's <c>JsonFormatter</c>
    /// serializes <c>int64</c> as a JSON string to avoid 53-bit precision loss, while
    /// System.Text.Json writes it as a JSON number; both encodings are matched.</summary>
    public static bool Matches(JsonElement element, long expected)
        => TryReadInt64(element, out var actual) && actual == expected;

    /// <summary>Reports whether <paramref name="element"/> is a JSON unsigned 64-bit integer equal
    /// to <paramref name="expected"/>, encoded either as a JSON <em>number</em> (read as an exact
    /// unsigned 64-bit integer via <see cref="JsonElement.TryGetUInt64(out ulong)"/>; <c>-0</c> reads as zero) or
    /// as a JSON <em>string</em> that holds a JSON integer literal in <see cref="ulong"/> range. A number that is
    /// fractional, negative, or out of <see cref="ulong"/> range, any other string, or any other kind returns
    /// <see langword="false"/>. Mirrors the <see cref="long"/> overload for unsigned values
    /// (Protobuf's <c>JsonFormatter</c> serializes <c>uint64</c> as a JSON string).</summary>
    public static bool Matches(JsonElement element, ulong expected)
        => TryReadUInt64(element, out var actual) && actual == expected;

    /// <summary>Reports whether <paramref name="element"/> is a JSON string equal (ordinal)
    /// to any of <paramref name="candidates"/>. <see langword="false"/> when the element is
    /// not a JSON string, or when none of the candidates matches.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="candidates"/> is <see langword="null"/>.</exception>
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
    /// out of scope for this overload, and a literal beyond <see cref="double"/> range never matches.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="candidates"/> is <see langword="null"/>.</exception>
    [SuppressMessage(
        "Major Bug",
        "S1244:Do not check floating point equality with exact values",
        Justification = "Exact equality is the documented contract of this overload: the caller supplies the candidate values to match. A tolerance would pass values the caller did not ask for, with no way to opt out.")]
    public static bool MatchesAny(JsonElement element, params double[] candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        if (!TryReadDouble(element, out var actual))
        {
            return false;
        }

        var capturedActual = actual;
        return Array.Exists(candidates, c => c.Equals(capturedActual));
    }

    /// <summary>Reports whether <paramref name="element"/> is a JSON 32-bit integer equal to any of
    /// <paramref name="candidates"/>, encoded either as a JSON <em>number</em> (read via
    /// <see cref="JsonElement.TryGetInt32(out int)"/>) or as a JSON <em>string</em> that
    /// holds a JSON integer literal in <see cref="int"/> range. A
    /// fractional or out-of-range number, any other string, or any other kind returns
    /// <see langword="false"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="candidates"/> is <see langword="null"/>.</exception>
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
    /// <see cref="JsonElement.TryGetUInt32(out uint)"/>) or as a JSON <em>string</em> that
    /// holds a JSON integer literal in <see cref="uint"/> range. A
    /// fractional, negative, or out-of-range number, any other string, or any other kind
    /// returns <see langword="false"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="candidates"/> is <see langword="null"/>.</exception>
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
    /// <see cref="JsonElement.TryGetInt64(out long)"/>) or as a JSON <em>string</em> that
    /// holds a JSON integer literal in <see cref="long"/> range. A
    /// fractional or out-of-range number, any other string, or any other kind returns
    /// <see langword="false"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="candidates"/> is <see langword="null"/>.</exception>
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
    /// <see cref="JsonElement.TryGetUInt64(out ulong)"/>) or as a JSON <em>string</em> that
    /// holds a JSON integer literal in <see cref="ulong"/> range. A
    /// fractional, negative, or out-of-range number, any other string, or any other kind
    /// returns <see langword="false"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="candidates"/> is <see langword="null"/>.</exception>
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

    /// <summary>Reads <paramref name="element"/> as a finite <see cref="double"/> from a JSON number.</summary>
    private static bool TryReadDouble(JsonElement element, out double value)
    {
        value = 0;
        return element.ValueKind is JsonValueKind.Number && element.TryGetDouble(out value) && double.IsFinite(value);
    }

    /// <summary>Reads <paramref name="element"/> as a 32-bit integer from either a JSON number or a
    /// JSON string that holds a JSON integer literal.</summary>
    private static bool TryReadInt32(JsonElement element, out int value)
    {
        value = 0;
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetInt32(out value),
            JsonValueKind.String => element.GetString() is { } raw && IsJsonIntegerLiteral(raw)
                && int.TryParse(raw, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value),
            _ => false,
        };
    }

    /// <summary>Reads <paramref name="element"/> as an unsigned 32-bit integer from either a JSON
    /// number (where <c>-0</c> is zero) or a JSON string that holds a JSON integer literal.</summary>
    private static bool TryReadUInt32(JsonElement element, out uint value)
    {
        value = 0;
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetUInt32(out value) || IsNegativeZero(element),
            JsonValueKind.String => element.GetString() is { } raw && IsJsonIntegerLiteral(raw)
                && uint.TryParse(raw, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value),
            _ => false,
        };
    }

    /// <summary>Reads <paramref name="element"/> as a 64-bit integer from either a JSON number or a
    /// JSON string that holds a JSON integer literal.</summary>
    private static bool TryReadInt64(JsonElement element, out long value)
    {
        value = 0;
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetInt64(out value),
            JsonValueKind.String => element.GetString() is { } raw && IsJsonIntegerLiteral(raw)
                && long.TryParse(raw, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value),
            _ => false,
        };
    }

    /// <summary>Reads <paramref name="element"/> as an unsigned 64-bit integer from either a JSON
    /// number (where <c>-0</c> is zero) or a JSON string that holds a JSON integer literal.</summary>
    private static bool TryReadUInt64(JsonElement element, out ulong value)
    {
        value = 0;
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetUInt64(out value) || IsNegativeZero(element),
            JsonValueKind.String => element.GetString() is { } raw && IsJsonIntegerLiteral(raw)
                && ulong.TryParse(raw, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value),
            _ => false,
        };
    }

    /// <summary>Reports whether the JSON number <paramref name="element"/> is the integer literal <c>-0</c>, which
    /// the unsigned readers of <see cref="JsonElement"/> reject.</summary>
    private static bool IsNegativeZero(JsonElement element)
        => string.Equals(element.GetRawText(), "-0", StringComparison.Ordinal);

    /// <summary>Reports whether <paramref name="text"/> is a JSON integer literal: an optional <c>-</c>, then
    /// <c>0</c> or a digit from <c>1</c> to <c>9</c> followed by digits. White space, a <c>+</c> and leading zeros
    /// are not part of it.</summary>
    internal static bool IsJsonIntegerLiteral(string text)
    {
        var digits = text.AsSpan(text.AsSpan().StartsWith('-') ? 1 : 0);
        return digits switch
        {
            [] => false,
            ['0'] => true,
            ['0', ..] => false,
            _ => !digits.ContainsAnyExceptInRange('0', '9'),
        };
    }
}
