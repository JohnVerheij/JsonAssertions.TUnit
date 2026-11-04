using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using TUnit.Assertions.Exceptions;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// End-to-end tests for <c>RoundtripsCleanlyVia&lt;T&gt;</c>: asserts that a value
/// serializes-deserializes-reserializes via a <c>JsonTypeInfo&lt;T&gt;</c> without drift in the
/// resulting JSON string. Covers the success path, missing-property regression (the educational-demand
/// use case the v0.3.0 plan calls out), and the deserialize-to-null edge.
/// </summary>
[Category("Smoke")]
[Timeout(5_000)]
internal sealed partial class RoundtripsCleanlyViaTests
{
    [Test]
    public async Task RoundtripsCleanlyVia_StableShape_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var dto = new RoundtripDto(42, "alice", true);

        await Assert.That(dto).RoundtripsCleanlyVia(RoundtripJsonContext.Default.RoundtripDto);
    }

    [Test]
    public async Task RoundtripsCleanlyVia_RecordWithNullableField_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var dto = new RoundtripDto(0, null, false);

        await Assert.That(dto).RoundtripsCleanlyVia(RoundtripJsonContext.Default.RoundtripDto);
    }

    [Test]
    public async Task RoundtripsCleanlyVia_NullValue_Passes(CancellationToken ct)
    {
        // Per the v0.3.0 behavior fix, a null payload legitimately survives the round-trip.
        ct.ThrowIfCancellationRequested();
        RoundtripDto? dto = null;

        await Assert.That(dto).RoundtripsCleanlyVia(RoundtripJsonContext.Default.RoundtripDto);
    }

    [Test]
    public async Task RoundtripsCleanlyVia_NullJsonTypeInfo_ThrowsArgumentNull(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var dto = new RoundtripDto(1, "x", true);
        JsonTypeInfo<RoundtripDto>? typeInfo = null;

        await Assert.That(async () =>
        {
            await Assert.That(dto).RoundtripsCleanlyVia(typeInfo!);
        }).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task RoundtripsCleanlyVia_DriftingSecondPass_FailsWithBothPayloads(CancellationToken ct)
    {
        // Custom converter writes the value differently on the second-pass serialize: read
        // canonical "{\"v\":N}" form, write back as "{\"v\":N,\"drift\":true}". The round-trip
        // detects the drift and the failure message renders both payloads side-by-side.
        ct.ThrowIfCancellationRequested();

        var ex = await Assert.That(async () =>
        {
            await Assert.That(new Drifter(7)).RoundtripsCleanlyVia(DrifterContext.Default.Drifter);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("drifted between trips");
        await Assert.That(ex.Message).Contains("first:");
        await Assert.That(ex.Message).Contains("second:");
    }

    [Test]
    public async Task RoundtripsCleanlyVia_DeserializationThrows_FailsWithParseError(CancellationToken ct)
    {
        // Custom converter writes invalid JSON on serialize; the assertion's deserialize step
        // throws JsonException and the failure surfaces the parse error + the first-pass JSON.
        ct.ThrowIfCancellationRequested();

        var ex = await Assert.That(async () =>
        {
            await Assert.That(new Unparseable()).RoundtripsCleanlyVia(UnparseableContext.Default.Unparseable);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("deserializing the first-pass JSON failed");
        await Assert.That(ex.Message).Contains("first-pass JSON:");
    }

    [Test]
    public async Task RoundtripsCleanlyVia_DeserializesToNullFromNonNull_FailsExplicitly(CancellationToken ct)
    {
        // Custom converter that serializes to a literal "null" payload from a non-null instance.
        // The assertion detects the value-was-non-null vs deserialized-to-null mismatch and renders
        // the dedicated diagnostic.
        ct.ThrowIfCancellationRequested();

        var ex = await Assert.That(async () =>
        {
            await Assert.That(new VanishesToNull()).RoundtripsCleanlyVia(VanishesContext.Default.VanishesToNull);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("deserializing the first-pass JSON produced null");
    }

    /// <summary>A type reachable from a registered type gets its own <c>JsonTypeInfo</c> property without
    /// an attribute; in an <see cref="object"/>-typed property, a runtime type the context does not
    /// register and that has no registered base type or interface throws
    /// <see cref="NotSupportedException"/> instead of failing the assertion.</summary>
    [Test]
    public async Task RoundtripsCleanlyVia_Registrations_ReachableTypeWorksUnknownRuntimeTypeThrows(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(new HolderPart(7)).RoundtripsCleanlyVia(HolderContext.Default.HolderPart);

        await Assert.That(async () =>
        {
            await Assert.That(new Holder(new HolderPart(1), new UnregisteredPart(2))).RoundtripsCleanlyVia(HolderContext.Default.Holder);
        }).Throws<NotSupportedException>();
    }

    /// <summary>A derived value held through a base type without polymorphic configuration is
    /// serialized with the base contract: the derived-only property is left out of both serialized
    /// forms, so the assertion passes without checking it.</summary>
    [Test]
    public async Task RoundtripsCleanlyVia_DerivedValueThroughBaseType_PassesWithoutDerivedProperties(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        BasePart value = new DerivedPart(3, "dropped");

        await Assert.That(JsonSerializer.Serialize(value, HolderContext.Default.BasePart)).IsEqualTo("""{"Value":3}""");
        await Assert.That(value).RoundtripsCleanlyVia(HolderContext.Default.BasePart);
    }

    /// <summary>In an <see cref="object"/>-typed property, a runtime type the context does not register
    /// is serialized with the contract of a registered base type or interface: the properties only it
    /// declares are left out of both serialized forms, so the assertion passes without checking them.</summary>
    [Test]
    public async Task RoundtripsCleanlyVia_UnregisteredTypeInObjectProperty_UsesRegisteredAncestorContract(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var viaBaseType = new Holder(new HolderPart(1), new DerivedPart(3, "dropped"));
        var viaInterface = new Holder(new HolderPart(1), new InterfacePart(4, "dropped"));

        await Assert.That(JsonSerializer.Serialize(viaBaseType, HolderContext.Default.Holder))
            .IsEqualTo("""{"Part":{"Value":1},"Extra":{"Value":3}}""");
        await Assert.That(viaBaseType).RoundtripsCleanlyVia(HolderContext.Default.Holder);
        await Assert.That(JsonSerializer.Serialize(viaInterface, HolderContext.Default.Holder))
            .IsEqualTo("""{"Part":{"Value":1},"Extra":{"Value":4}}""");
        await Assert.That(viaInterface).RoundtripsCleanlyVia(HolderContext.Default.Holder);
    }

    /// <summary>An <see cref="object"/>-typed property deserializes to a <see cref="JsonElement"/> by
    /// default, so a non-null value in it makes the second serialization throw
    /// <see cref="NotSupportedException"/> unless the context registers <see cref="JsonElement"/>. The
    /// value here has a registered type, so the first serialization succeeds.</summary>
    [Test]
    public async Task RoundtripsCleanlyVia_ObjectPropertyWithoutJsonElementRegistration_Throws(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var holder = new Holder(new HolderPart(1), new HolderPart(2));

        await Assert.That(JsonSerializer.Serialize(holder, HolderWithoutJsonElementContext.Default.Holder))
            .IsEqualTo("""{"Part":{"Value":1},"Extra":{"Value":2}}""");
        var ex = await Assert.That(async () =>
        {
            await Assert.That(holder).RoundtripsCleanlyVia(HolderWithoutJsonElementContext.Default.Holder);
        }).Throws<NotSupportedException>();
        await Assert.That(ex!.Message).Contains("System.Text.Json.JsonElement");
        await Assert.That(holder).RoundtripsCleanlyVia(HolderContext.Default.Holder);
    }

    /// <summary>A registered type with a property type that has no attribute of its own, and an
    /// <see cref="object"/>-typed property.</summary>
    internal sealed record Holder(HolderPart Part, object? Extra);

    /// <summary>Reachable from <see cref="Holder"/> only.</summary>
    internal sealed record HolderPart(int Value);

    /// <summary>Not reachable from any registration.</summary>
    internal sealed record UnregisteredPart(int Value);

    /// <summary>A base type without polymorphic configuration.</summary>
    internal record BasePart(int Value);

    /// <summary>Adds a property that the contract of <see cref="BasePart"/> does not have.</summary>
    internal sealed record DerivedPart(int Value, string Extra) : BasePart(Value);

    /// <summary>An interface that the context registers.</summary>
    internal interface IPart
    {
        /// <summary>The only property in the contract of the interface.</summary>
        int Value { get; }
    }

    /// <summary>Implements <see cref="IPart"/> and adds a property that its contract does not have.</summary>
    internal sealed record InterfacePart(int Value, string Extra) : IPart;

    [JsonSerializable(typeof(Holder))]
    [JsonSerializable(typeof(BasePart))]
    [JsonSerializable(typeof(IPart))]
    [JsonSerializable(typeof(JsonElement))]
    internal sealed partial class HolderContext : JsonSerializerContext;

    /// <summary>Registers <see cref="Holder"/> but not <see cref="JsonElement"/>.</summary>
    [JsonSerializable(typeof(Holder))]
    internal sealed partial class HolderWithoutJsonElementContext : JsonSerializerContext;

    /// <summary>A small record used as the round-trip target. Records get value-equality
    /// for free; STJ source-gen produces a stable JSON shape for them in v0.3.0.</summary>
    internal sealed record RoundtripDto(int Id, string? Name, bool IsActive);

    /// <summary>STJ source-gen context for <see cref="RoundtripDto"/>. The outer
    /// <see cref="RoundtripsCleanlyViaTests"/> class is partial so the source generator can emit
    /// the generated half of this context next to its declaration (the same workaround used in
    /// <c>HasJsonResponseTests</c>).</summary>
    [JsonSerializable(typeof(RoundtripDto))]
    internal sealed partial class RoundtripJsonContext : JsonSerializerContext;

    /// <summary>A type whose JSON shape drifts on the second-pass serialize via a custom
    /// converter. Used to exercise <c>RoundtripMismatch</c> diagnostics.</summary>
    [JsonConverter(typeof(DrifterConverter))]
    internal sealed record Drifter(int V);

    internal sealed class DrifterConverter : System.Text.Json.Serialization.JsonConverter<Drifter>
    {
        private int _writeCount;

        public override Drifter Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            return new Drifter(doc.RootElement.GetProperty("v").GetInt32());
        }

        public override void Write(Utf8JsonWriter writer, Drifter value, JsonSerializerOptions options)
        {
            _writeCount++;
            writer.WriteStartObject();
            writer.WriteNumber("v", value.V);
            if (_writeCount >= 2)
            {
                writer.WriteBoolean("drift", true);
            }
            writer.WriteEndObject();
        }
    }

    [JsonSerializable(typeof(Drifter))]
    internal sealed partial class DrifterContext : JsonSerializerContext;

    /// <summary>A type whose serialized form is intentionally invalid JSON. Used to exercise
    /// <c>RoundtripDeserializationFailed</c> diagnostics.</summary>
    [JsonConverter(typeof(UnparseableConverter))]
    internal sealed record Unparseable;

    internal sealed class UnparseableConverter : System.Text.Json.Serialization.JsonConverter<Unparseable>
    {
        public override Unparseable Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => new();

        public override void Write(Utf8JsonWriter writer, Unparseable value, JsonSerializerOptions options)
        {
            // Emit a raw fragment that breaks subsequent Deserialize calls.
            writer.WriteRawValue("{not-json", skipInputValidation: true);
        }
    }

    [JsonSerializable(typeof(Unparseable))]
    internal sealed partial class UnparseableContext : JsonSerializerContext;

    /// <summary>A type that serializes from a non-null instance to the literal <c>"null"</c>
    /// payload, exercising the non-null-but-deserialized-to-null diagnostic.</summary>
    [JsonConverter(typeof(VanishesConverter))]
    internal sealed record VanishesToNull;

    internal sealed class VanishesConverter : System.Text.Json.Serialization.JsonConverter<VanishesToNull>
    {
        public override VanishesToNull? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => null;

        public override void Write(Utf8JsonWriter writer, VanishesToNull value, JsonSerializerOptions options)
            => writer.WriteNullValue();
    }

    [JsonSerializable(typeof(VanishesToNull))]
    internal sealed partial class VanishesContext : JsonSerializerContext;
}
