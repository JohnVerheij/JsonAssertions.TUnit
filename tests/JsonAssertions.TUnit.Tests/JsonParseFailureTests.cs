using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TUnit.Assertions.Exceptions;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// Pins that every JSON-<see cref="string"/> assertion overload fails with an explained
/// message when the input is not valid JSON, rather than letting a raw
/// <see cref="JsonException"/> escape. The negative assertion is included deliberately: a body
/// that cannot be parsed must not vacuously satisfy <c>DoesNotHaveJsonProperty</c>.
/// </summary>
[Category("Smoke")]
[Timeout(5_000)]
internal sealed class JsonParseFailureTests
{
    private const string Malformed = "{ \"user\": ";

    [Test]
    public async Task HasJsonProperty_MalformedJson_FailsWithParseMessage(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That(Malformed).HasJsonProperty("user");
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("the asserted value to be parseable JSON");
        await Assert.That(ex.Message).Contains("but parsing failed:");
    }

    [Test]
    public async Task DoesNotHaveJsonProperty_MalformedJson_Fails(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // A body that cannot be parsed must NOT vacuously satisfy the negative assertion.
        var ex = await Assert.That(async () =>
        {
            await Assert.That(Malformed).DoesNotHaveJsonProperty("user");
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("but parsing failed:");
    }

    [Test]
    public async Task HasJsonValue_MalformedJson_FailsWithParseMessage(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That(Malformed).HasJsonValue("user", "alice");
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("but parsing failed:");
    }

    [Test]
    public async Task HasJsonArrayLength_MalformedJson_FailsWithParseMessage(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That(Malformed).HasJsonArrayLength("user", 1);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("but parsing failed:");
    }

    [Test]
    public async Task HasJsonValueKind_MalformedJson_FailsWithParseMessage(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That(Malformed).HasJsonValueKind("user", JsonValueKind.Object);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("but parsing failed:");
    }
}
