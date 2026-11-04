using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using JsonAssertions;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace JsonAssertions.TUnit;

/// <summary>
/// Fluent assertion that a typed value round-trips cleanly through STJ source-gen via the
/// consumer's <see cref="JsonTypeInfo{T}"/>. It reports a drift between the first and the second
/// serialized form, a first-pass JSON that fails to deserialize with a
/// <see cref="JsonException"/>, and a non-null value that deserializes back to
/// <see langword="null"/>.
/// </summary>
/// <remarks>
/// <para>Mechanism: <c>Serialize(value, jsonTypeInfo)</c> -&gt; <c>Deserialize(json1, jsonTypeInfo)</c>
/// -&gt; <c>Serialize(roundtripped, jsonTypeInfo)</c>. The two serialized strings are compared
/// ordinally; if they differ, the round-trip is lossy and the assertion fails with both
/// JSON strings rendered for diagnosis.</para>
/// <para>AOT-clean: the supplied <see cref="JsonTypeInfo{T}"/> is the source-generated entry
/// for <c>T</c> in the consumer's <c>JsonSerializerContext</c>; the assertion
/// uses no runtime reflection.</para>
/// <para>Use case: consumers shipping
/// AOT-compatible web APIs typically have a <c>JsonSerializerContext</c> but rarely write
/// regression tests verifying that EVERY type in the context round-trips correctly. A
/// <c>RoundtripsCleanlyVia</c> regression test for each type in each context is a 1-2 hour
/// sweep that catches future round-trip drift at CI time.</para>
/// <para>Registrations: the source generator also generates a <see cref="JsonTypeInfo{T}"/> for every
/// type reachable from a <c>[JsonSerializable]</c> type, such as a property type, and the context
/// exposes it as a property, so <c>T</c> needs no attribute of its own when it is reachable. Use
/// <c>HasJsonTypeInfoFor</c> to check what a context registers.</para>
/// <para>Derived types: a derived value held through a base type (as <c>T</c> or as a property type)
/// is serialized with the contract of the base type unless the base type is configured for
/// polymorphism with <c>[JsonDerivedType]</c>. The properties that only the derived type declares
/// are then left out of both serialized forms, so the assertion passes without checking them.</para>
/// <para><see cref="object"/>-typed properties: a value whose runtime type the context does not
/// register is serialized with the contract of a registered base type or interface, which leaves out
/// the properties only the runtime type declares in the same way; with no registered ancestor besides
/// <see cref="object"/>, the serializer throws <see cref="NotSupportedException"/>. The property
/// deserializes to a <see cref="JsonElement"/> by default, so any non-null value also makes the second
/// serialization throw <see cref="NotSupportedException"/> unless the context registers
/// <see cref="JsonElement"/> (<c>[JsonSerializable(typeof(JsonElement))]</c>). This assertion does not
/// catch these exceptions, so the test errors rather than fails.</para>
/// </remarks>
[SuppressMessage(
    "Usage",
    "VSTHRD200:Use \"Async\" suffix for async methods",
    Justification = "This is a [GenerateAssertion] source method: the method name becomes the fluent chain entry point (Assert.That(value).RoundtripsCleanlyVia(...)), so an Async suffix would corrupt the assertion surface. The method is synchronous anyway (no IO, no await).")]
public static class JsonRoundtripAssertions
{
    /// <summary>Asserts the value round-trips cleanly through the supplied
    /// <see cref="JsonTypeInfo{T}"/>: serializing it produces a JSON string that, when
    /// deserialized and re-serialized via the same context, yields a byte-identical JSON
    /// string. Catches output that drifts on round-trip (for example an asymmetric converter or a
    /// property ordering that changes) and serializer-options-vs-context inconsistencies. A value
    /// serialized with the contract of a base type or interface (a derived value held through a base
    /// type without polymorphic configuration, or an unregistered type in an <see cref="object"/>-typed
    /// property) passes without its own properties being checked. An unregistered type with no
    /// registered ancestor besides <see cref="object"/>, or any non-null value in an
    /// <see cref="object"/>-typed property when the context does not register <see cref="JsonElement"/>,
    /// throws <see cref="NotSupportedException"/> instead of failing the assertion.</summary>
    /// <typeparam name="T">The value type whose round-trip is asserted.</typeparam>
    /// <param name="value">The value to round-trip.</param>
    /// <param name="jsonTypeInfo">The <see cref="JsonTypeInfo{T}"/> from the consumer's
    /// <c>JsonSerializerContext</c>; AOT-clean.</param>
    [GenerateAssertion]
    public static AssertionResult RoundtripsCleanlyVia<T>(this T value, JsonTypeInfo<T> jsonTypeInfo)
    {
        ArgumentNullException.ThrowIfNull(jsonTypeInfo);

        var json1 = JsonSerializer.Serialize(value, jsonTypeInfo);

        T? roundtripped;
        try
        {
            roundtripped = JsonSerializer.Deserialize(json1, jsonTypeInfo);
        }
        catch (JsonException ex)
        {
            return AssertionResult.Failed(JsonFailureMessage.RoundtripDeserializationFailed(json1, ex));
        }

        // A null input legitimately round-trips through STJ as "null" → null → "null"; only flag the
        // pathological case where a non-null value deserialized back to null (serializer corruption).
        if (value is not null && roundtripped is null)
        {
            return AssertionResult.Failed(JsonFailureMessage.RoundtripDeserializedToNull(json1));
        }

        var json2 = JsonSerializer.Serialize(roundtripped!, jsonTypeInfo);

        return string.Equals(json1, json2, StringComparison.Ordinal)
            ? AssertionResult.Passed
            : AssertionResult.Failed(JsonFailureMessage.RoundtripMismatch(json1, json2));
    }
}
