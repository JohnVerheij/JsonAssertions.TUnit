using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace JsonAssertions;

/// <summary>
/// Produces a deterministic, structural canonical form of a JSON document: object keys sorted
/// ordinally, stable two-space indentation, LF line endings, and every value preserved (so a new
/// or removed field surfaces as a text diff), with optional JSON-path scrubbing of volatile fields.
/// Unlike <see cref="JsonRenderers.ReformatJson{T}"/> this needs no typed <c>JsonSerializerContext</c>
/// and keeps unknown properties, which is what makes it suitable for pinning the whole shape of a
/// response as a snapshot baseline.
/// </summary>
/// <remarks>
/// Pairs with a snapshot library's normalizer hook (for example SnapshotAssertions'
/// <c>SnapshotOptions.WithNormalizer</c>): the consumer composes the two at its own call site, so
/// neither package depends on the other.
/// <code>
/// await Assert.That(body).MatchesSnapshot(SnapshotOptions.Default
///     .WithNormalizer(s => JsonCanonicalizer.Canonicalize(s, o => o
///         .ScrubPath("grpc.serverUri")
///         .ScrubPath("[*].eventBus.connectionInfo"))));
/// </code>
/// </remarks>
public static class JsonCanonicalizer
{
    /// <summary>Canonicalizes <paramref name="json"/> with no scrubbing: sorted keys, stable
    /// indentation, all values preserved.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <returns>The canonical form.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is <see langword="null"/>.</exception>
    /// <exception cref="JsonException"><paramref name="json"/> is not valid JSON.</exception>
    public static string Canonicalize(string json) => Canonicalize(json, configure: null);

    /// <summary>Canonicalizes <paramref name="json"/>, applying the scrubbing configured by
    /// <paramref name="configure"/>: sorted keys, stable indentation, all values preserved, and
    /// each registered scrub path's value replaced with the scrub token.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="configure">A callback that registers scrub paths and/or the scrub token. May be
    /// <see langword="null"/> for no scrubbing.</param>
    /// <returns>The canonical, scrubbed form.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is <see langword="null"/>.</exception>
    /// <exception cref="JsonException"><paramref name="json"/> is not valid JSON.</exception>
    public static string Canonicalize(string json, Action<JsonCanonicalizeOptions>? configure)
    {
        ArgumentNullException.ThrowIfNull(json);

        var options = new JsonCanonicalizeOptions();
        configure?.Invoke(options);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var scrubPaths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var pattern in options.ScrubPaths)
        {
            foreach (var resolution in JsonPath.ResolveAll(root, pattern))
            {
                if (resolution.Found)
                {
                    scrubPaths.Add(resolution.ResolvedPrefix);
                }
            }
        }

        var buffer = new ArrayBufferWriter<byte>();
        // Relaxed escaping keeps the canonical text readable for snapshot baselines: '<', '>',
        // '&', '+' and non-ASCII render as themselves rather than as \uXXXX escapes. This is a
        // test-time canonical form, never served to a browser, so HTML-escaping buys nothing.
        using (var writer = new Utf8JsonWriter(
            buffer,
            new JsonWriterOptions { Indented = true, NewLine = "\n", Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
        {
            WriteCanonical(writer, root, string.Empty, scrubPaths, options.ScrubToken);
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static void WriteCanonical(Utf8JsonWriter writer, JsonElement element, string path, HashSet<string> scrubPaths, string scrubToken)
    {
        if (scrubPaths.Contains(path))
        {
            writer.WriteStringValue(scrubToken);
            return;
        }

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                WriteCanonicalObject(writer, element, path, scrubPaths, scrubToken);
                break;
            case JsonValueKind.Array:
                WriteCanonicalArray(writer, element, path, scrubPaths, scrubToken);
                break;
            default:
                // Primitives (string / number / true / false / null) are written verbatim: a
                // canonical form preserves the value, it does not reformat numbers or strings.
                element.WriteTo(writer);
                break;
        }
    }

    private static void WriteCanonicalObject(Utf8JsonWriter writer, JsonElement element, string path, HashSet<string> scrubPaths, string scrubToken)
    {
        var properties = new List<JsonProperty>();
        foreach (var property in element.EnumerateObject())
        {
            properties.Add(property);
        }

        properties.Sort(static (a, b) => string.CompareOrdinal(a.Name, b.Name));

        writer.WriteStartObject();
        foreach (var property in properties)
        {
            writer.WritePropertyName(property.Name);
            var childPath = path.Length is 0 ? property.Name : string.Concat(path, ".", property.Name);
            WriteCanonical(writer, property.Value, childPath, scrubPaths, scrubToken);
        }

        writer.WriteEndObject();
    }

    private static void WriteCanonicalArray(Utf8JsonWriter writer, JsonElement element, string path, HashSet<string> scrubPaths, string scrubToken)
    {
        writer.WriteStartArray();
        var i = 0;
        foreach (var item in element.EnumerateArray())
        {
            var childPath = string.Concat(path, "[", i.ToString(CultureInfo.InvariantCulture), "]");
            WriteCanonical(writer, item, childPath, scrubPaths, scrubToken);
            i++;
        }

        writer.WriteEndArray();
    }
}
