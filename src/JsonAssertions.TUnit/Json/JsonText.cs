using System;
using System.Text.Json;

namespace JsonAssertions;

/// <summary>Reads JSON text the same way at every entry point that takes a <see cref="string"/>: one leading byte
/// order mark (U+FEFF), which RFC 8259 section 8.1 lets a parser ignore, is skipped. <c>Encoding.UTF8.GetString</c>
/// keeps the mark that <c>File.ReadAllText</c> and <c>HttpContent.ReadAsStringAsync</c> remove.</summary>
internal static class JsonText
{
    private const char ByteOrderMark = '\uFEFF';

    /// <summary>Parses <paramref name="json"/>, skipping one leading byte order mark.</summary>
    /// <param name="json">The JSON text.</param>
    /// <returns>The parsed document; the caller disposes it.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is <see langword="null"/>.</exception>
    /// <exception cref="JsonException"><paramref name="json"/> is not valid JSON.</exception>
    public static JsonDocument Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        return JsonDocument.Parse(json.AsMemory(Start(json)));
    }

    /// <summary>The text of <paramref name="json"/> without one leading byte order mark, for a reader that takes the
    /// text itself, such as <see cref="JsonSerializer"/>.</summary>
    /// <param name="json">The JSON text.</param>
    /// <returns>The text after the mark, or all of it.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is <see langword="null"/>.</exception>
    public static ReadOnlySpan<char> Content(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        return json.AsSpan(Start(json));
    }

    private static int Start(string json) => json.AsSpan().StartsWith(ByteOrderMark) ? 1 : 0;
}
