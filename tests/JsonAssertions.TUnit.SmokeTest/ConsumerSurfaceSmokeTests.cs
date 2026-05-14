namespace Smoke.Consumer;

/// <summary>
/// Smoke tests proving that an external consumer can adopt JsonAssertions.TUnit with no
/// extra <c>using JsonAssertions.TUnit;</c> directive at the call site and no other wiring.
/// The test class lives in <c>Smoke.Consumer</c> deliberately: JsonAssertions.TUnit's own
/// test project is in the <c>JsonAssertions.TUnit.Tests</c> namespace, which inherits
/// parent-namespace visibility into <c>JsonAssertions.TUnit</c>. That inheritance would mask
/// any future namespace-resolution bug in the source-generated entry points. By placing this
/// file in a namespace with NO parent relationship to JsonAssertions.TUnit, this project is
/// the canonical regression coverage for the resolution-pathway bug class.
/// </summary>
[Category("ConsumerSurface")]
[Timeout(10_000)]
internal sealed class ConsumerSurfaceSmokeTests
{
    private const string SampleJson = """{"user":{"name":"alice"}}""";

    /// <summary>
    /// Pins that <c>HasJsonProperty</c> on a JSON <see cref="string"/> resolves cleanly for an
    /// external consumer: the source-generator-emitted entry point in
    /// <c>TUnit.Assertions.Extensions</c> auto-imports alongside <c>Assert.That</c>.
    /// </summary>
    [Test]
    public async Task HasJsonPropertyOnStringResolvesAndPassesAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(SampleJson).HasJsonProperty("user.name");
    }

    /// <summary>
    /// Pins that the negative <c>DoesNotHaveJsonProperty</c> entry point resolves and passes
    /// for an absent path.
    /// </summary>
    [Test]
    public async Task DoesNotHaveJsonPropertyOnStringResolvesAndPassesAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(SampleJson).DoesNotHaveJsonProperty("user.email");
    }

    /// <summary>
    /// Pins that the <see cref="JsonElement"/> overload of <c>HasJsonProperty</c> resolves
    /// cleanly: the entry point is generated onto the <c>JsonElement</c> assertion source as
    /// well as the <c>string</c> one.
    /// </summary>
    [Test]
    public async Task HasJsonPropertyOnJsonElementResolvesAndPassesAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse(SampleJson);

        await Assert.That(document.RootElement).HasJsonProperty("user.name");
    }
}
