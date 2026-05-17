using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace JsonAssertions;

/// <summary>
/// Static factories returning JSON-string renderers for composition with framework-agnostic
/// snapshot or comparison consumers. Each method produces a <see cref="Func{T, TResult}"/>
/// of <see cref="string"/> to <see cref="string"/> that maps a JSON document into a canonical
/// re-serialized form via the consumer's <see cref="JsonTypeInfo{T}"/>. The output stability is
/// the consumer's <c>JsonSerializerContext</c>'s stability — STJ source-gen produces deterministic
/// property ordering, so the canonical form is reliable across runs.
/// </summary>
/// <remarks>
/// <para>Used to compose with <c>SnapshotAssertions</c>'s <c>MatchesSnapshot(Func&lt;string, string&gt;)</c>
/// overload (or any other framework-agnostic consumer of <c>Func&lt;string, string&gt;</c>). The
/// projection-helper pattern keeps <c>JsonAssertions</c> decoupled from <c>SnapshotAssertions</c> — the
/// composition happens at the consumer's call site via standard delegate types, per the
/// family's CONVENTIONS.md v0.6 cross-package references rule.</para>
/// <para>Two-step composition (async body-read in test code + sync renderer here):</para>
/// <code>
/// var body = await response.Content.ReadAsStringAsync(ct);
/// await Assert.That(body).MatchesSnapshot(JsonRenderers.ReformatJson(MyJsonContext.Default.MyDto));
/// </code>
/// <para>The two-step pattern (async body-read + sync renderer) honors the family's
/// "no sync-over-async" rule: the renderer is purely synchronous (parsing strings, not reading
/// streams). HttpResponseMessage body-reading remains async in the consumer's code.</para>
/// <para>Lives in the <c>JsonAssertions</c> namespace ("core" namespace; framework-agnostic)
/// rather than <c>JsonAssertions.TUnit</c> (adapter), because the helper returns standard
/// BCL delegate types and depends only on <c>System.Text.Json</c>. A hypothetical future
/// non-TUnit framework adapter could compose with the same renderer.</para>
/// </remarks>
public static class JsonRenderers
{
    /// <summary>Returns a synchronous renderer that takes a JSON string, deserializes it via the
    /// supplied <paramref name="jsonTypeInfo"/>, and re-serializes the result via the same
    /// <see cref="JsonTypeInfo{T}"/>. The output is the canonical form per the consumer's
    /// <c>JsonSerializerContext</c> options: property ordering follows the context's source-gen
    /// emission order, and whitespace / naming / null handling are inherited from the supplied
    /// context. AOT-clean: no runtime reflection, only the supplied <see cref="JsonTypeInfo{T}"/>'s
    /// pre-generated metadata.</summary>
    /// <typeparam name="T">The expected JSON document type.</typeparam>
    /// <param name="jsonTypeInfo">The source-generated <see cref="JsonTypeInfo{T}"/> from the
    /// consumer's <c>JsonSerializerContext</c>.</param>
    /// <returns>A reusable sync renderer that canonicalises a JSON string.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="jsonTypeInfo"/> is <see langword="null"/>.</exception>
    public static Func<string, string> ReformatJson<T>(JsonTypeInfo<T> jsonTypeInfo)
    {
        ArgumentNullException.ThrowIfNull(jsonTypeInfo);
        return json =>
        {
            ArgumentNullException.ThrowIfNull(json);
            var value = JsonSerializer.Deserialize(json, jsonTypeInfo);
            // null deserialization result canonicalizes to the literal "null" JSON token
            // (STJ overload-set mismatch on nullable reference types is the reason for the
            // explicit branch rather than passing T? directly to Serialize).
            return value is null ? "null" : JsonSerializer.Serialize(value, jsonTypeInfo);
        };
    }
}
