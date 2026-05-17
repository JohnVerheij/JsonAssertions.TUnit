using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using TUnit.Assertions.Exceptions;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// Tests for <c>HasJsonTypeInfoFor&lt;T&gt;</c>: asserts that a
/// <c>JsonSerializerContext</c> registers a <c>JsonTypeInfo&lt;T&gt;</c> for the asserted
/// type, catching the "added a domain type but forgot the <c>[JsonSerializable(typeof(T))]</c>"
/// regression class. The educational-demand AOT-moat companion to
/// <c>RoundtripsCleanlyVia</c>.
/// </summary>
[Category("Smoke")]
[Timeout(5_000)]
internal sealed partial class HasJsonTypeInfoForTests
{
    [Test]
    public async Task HasJsonTypeInfoFor_RegisteredType_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(ContextRegistrationJsonContext.Default)
            .AsJsonContext()
            .HasJsonTypeInfoFor<RegisteredDto>();
    }

    [Test]
    public async Task HasJsonTypeInfoFor_UnregisteredType_FailsWithHint(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // System.DateTime is intentionally not in the test context. Asserting it must fail
        // with a hint pointing at the missing [JsonSerializable(typeof(DateTime))] attribute.
        var ex = await Assert.That(async () =>
        {
            await Assert.That(ContextRegistrationJsonContext.Default)
                .AsJsonContext()
                .HasJsonTypeInfoFor<System.DateTime>();
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("DateTime");
        await Assert.That(ex.Message).Contains("ContextRegistrationJsonContext");
        await Assert.That(ex.Message).Contains("[JsonSerializable(typeof(DateTime))]");
    }

    [Test]
    public async Task HasJsonTypeInfoFor_NullContext_FailsWithNullDiagnostic(CancellationToken ct)
    {
        // Edge case: the receiver itself is a null JsonSerializerContext. AsJsonContext wraps it
        // into the IJsonContextAssertionSource; the leaf assertion's CheckAsync sees a null value
        // and surfaces the "Actual value is null" branch.
        ct.ThrowIfCancellationRequested();
        ContextRegistrationJsonContext? nullContext = null;

        var ex = await Assert.That(async () =>
        {
            await Assert.That(nullContext).AsJsonContext().HasJsonTypeInfoFor<RegisteredDto>();
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("null");
    }

    [Test]
    public async Task HasJsonTypeInfoFor_LazyEvaluationThrows_FailsWithThrewDiagnostic(CancellationToken ct)
    {
        // Edge case: the receiver is a Func that throws during lazy evaluation. The leaf
        // assertion's CheckAsync sees metadata.Exception set and surfaces the "threw ..." branch.
        ct.ThrowIfCancellationRequested();

        var ex = await Assert.That(async () =>
        {
            await Assert.That(() => ThrowingContextFactory()).AsJsonContext().HasJsonTypeInfoFor<RegisteredDto>();
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("threw");
        await Assert.That(ex.Message).Contains("InvalidOperationException");

        static ContextRegistrationJsonContext ThrowingContextFactory()
            => throw new System.InvalidOperationException("simulated evaluation failure");
    }

    /// <summary>A domain type registered in the test context.</summary>
    internal sealed record RegisteredDto(int Id, string Name);

    /// <summary>STJ source-gen context registering only <see cref="RegisteredDto"/>.
    /// Other types asserted against this context (e.g. <see cref="System.DateTime"/> in the
    /// failure-path test) are intentionally not registered so the assertion can fail.</summary>
    [JsonSerializable(typeof(RegisteredDto))]
    internal sealed partial class ContextRegistrationJsonContext : JsonSerializerContext;
}
