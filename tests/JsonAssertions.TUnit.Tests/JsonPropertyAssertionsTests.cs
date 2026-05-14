using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TUnit.Assertions.Exceptions;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// End-to-end tests for the <c>[GenerateAssertion]</c>-emitted fluent chains:
/// <c>HasJsonProperty</c> and <c>DoesNotHaveJsonProperty</c> on both a JSON
/// <see cref="string"/> and a <see cref="JsonElement"/>. Each assertion has one passing
/// case and one failing case so the generated chain and its expectation message are both
/// exercised.
/// </summary>
[Category("Smoke")]
[Timeout(5_000)]
internal sealed class JsonPropertyAssertionsTests
{
    private const string SampleJson = """{"user":{"name":"alice"}}""";

    [Test]
    public async Task HasJsonProperty_String_PropertyPresent_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(SampleJson).HasJsonProperty("user.name");
    }

    [Test]
    public async Task HasJsonProperty_String_PropertyAbsent_Fails(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(async () =>
        {
            await Assert.That(SampleJson).HasJsonProperty("user.email");
        }).Throws<AssertionException>();
    }

    [Test]
    public async Task HasJsonProperty_JsonElement_PropertyPresent_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse(SampleJson);

        await Assert.That(document.RootElement).HasJsonProperty("user.name");
    }

    [Test]
    public async Task DoesNotHaveJsonProperty_String_PropertyAbsent_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(SampleJson).DoesNotHaveJsonProperty("user.email");
    }

    [Test]
    public async Task DoesNotHaveJsonProperty_String_PropertyPresent_Fails(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(async () =>
        {
            await Assert.That(SampleJson).DoesNotHaveJsonProperty("user.name");
        }).Throws<AssertionException>();
    }

    [Test]
    public async Task DoesNotHaveJsonProperty_JsonElement_PropertyAbsent_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse(SampleJson);

        await Assert.That(document.RootElement).DoesNotHaveJsonProperty("user.email");
    }
}
