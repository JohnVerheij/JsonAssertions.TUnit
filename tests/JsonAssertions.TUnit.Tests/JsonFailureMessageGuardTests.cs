using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// The public <see cref="JsonFailureMessage"/> factories check the resolution they render: a property-not-found
/// failure needs a failed resolution, a should-not-exist failure a found one, and the value and shape failures any
/// resolution <see cref="JsonPath"/> returns, but not a <see langword="default"/> one. On 0.6.2 a wrong resolution threw
/// <see cref="NullReferenceException"/> or rendered "an undefined value exists".
/// </summary>
[Timeout(5_000)]
internal sealed class JsonFailureMessageGuardTests
{
    private const string Json = """{"user":{"name":"alice"}}""";

    [Test]
    public async Task PropertyNotFound_RejectsAFoundOrDefaultResolution(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse(Json);
        var found = JsonPath.Resolve(document.RootElement, "user");

        await Assert.That(() => JsonFailureMessage.PropertyNotFound("user", found))
            .Throws<ArgumentException>().WithMessageContaining("this one is found", StringComparison.Ordinal);
        await Assert.That(() => JsonFailureMessage.PropertyNotFound("a", default))
            .Throws<ArgumentException>().WithMessageContaining("a default resolution", StringComparison.Ordinal);
        await Assert.That(JsonFailureMessage.PropertyNotFound("user.email", JsonPath.Resolve(document.RootElement, "user.email")))
            .IsEqualTo("resolved as far as 'user', which has no property 'email'");
    }

    [Test]
    public async Task PropertyShouldNotExist_RejectsANotFoundResolution(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse(Json);

        await Assert.That(() => JsonFailureMessage.PropertyShouldNotExist("nick", JsonPath.Resolve(document.RootElement, "nick")))
            .Throws<ArgumentException>();
        await Assert.That(() => JsonFailureMessage.PropertyShouldNotExist("nick", default))
            .Throws<ArgumentException>();
        await Assert.That(JsonFailureMessage.PropertyShouldNotExist("user", JsonPath.Resolve(document.RootElement, "user")))
            .IsEqualTo("found an Object");
    }

    [Test]
    public async Task ValueAndShapeMismatch_RejectADefaultResolution(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse(Json);

        await Assert.That(() => JsonFailureMessage.ValueMismatch("a", default, "1")).Throws<ArgumentException>();
        await Assert.That(() => JsonFailureMessage.ShapeMismatch("a", default, "an array")).Throws<ArgumentException>();
        await Assert.That(JsonFailureMessage.ValueMismatch("user.name", JsonPath.Resolve(document.RootElement, "user.name"), "\"bob\""))
            .IsEqualTo("found \"alice\"");
        await Assert.That(JsonFailureMessage.ShapeMismatch("user.nick", JsonPath.Resolve(document.RootElement, "user.nick"), "an array"))
            .IsEqualTo("resolved as far as 'user', which has no property 'nick'");
    }
}
