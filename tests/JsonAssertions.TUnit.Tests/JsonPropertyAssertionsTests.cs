using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TUnit.Assertions.Exceptions;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// End-to-end tests for the <c>[GenerateAssertion]</c>-emitted property-existence chains:
/// <c>HasJsonProperty</c> and <c>DoesNotHaveJsonProperty</c> over a JSON <see cref="string"/>
/// and a <see cref="JsonElement"/>. The failing cases inspect the exception message so the
/// path-context rendering in <c>JsonFailureMessage</c> (resolved-prefix tracking, the
/// object-vs-non-object reason, the root-vs-nested location) is exercised end-to-end.
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
    public async Task HasJsonProperty_JsonElement_PropertyPresent_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse(SampleJson);

        await Assert.That(document.RootElement).HasJsonProperty("user.name");
    }

    [Test]
    public async Task HasJsonProperty_MissingOnNestedObject_FailsWithPrefixAndSegment(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That(SampleJson).HasJsonProperty("user.email");
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("but resolved as far as 'user', which has no property 'email'");
    }

    [Test]
    public async Task HasJsonProperty_MissingOnRoot_FailsWithRootLocation(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That(SampleJson).HasJsonProperty("account");
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("but resolved as far as the root, which has no property 'account'");
    }

    [Test]
    public async Task HasJsonProperty_TraversesNonObject_FailsWithCannotReadReason(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That(SampleJson).HasJsonProperty("user.name.first");
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("but resolved as far as 'user.name', which is a String, not an object");
        await Assert.That(ex.Message).Contains("is a String, not an object");
    }

    [Test]
    public async Task HasJsonProperty_RootIsNotAnObject_FailsWithRootKind(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That("[1,2,3]").HasJsonProperty("user");
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("but resolved as far as the root, which is an Array, not an object");
    }

    [Test]
    public async Task DoesNotHaveJsonProperty_String_PropertyAbsent_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(SampleJson).DoesNotHaveJsonProperty("user.email");
    }

    [Test]
    public async Task DoesNotHaveJsonProperty_JsonElement_PropertyAbsent_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse(SampleJson);

        await Assert.That(document.RootElement).DoesNotHaveJsonProperty("user.email");
    }

    [Test]
    public async Task DoesNotHaveJsonProperty_PropertyPresent_FailsNamingFoundKind(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That(SampleJson).DoesNotHaveJsonProperty("user.name");
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("but found \"alice\"");
    }

    /// <summary><c>DoesNotHaveJsonProperty</c> does not expand <c>[*]</c>, so it rejects the wildcard up front with
    /// <see cref="System.ArgumentException"/>, whether or not the document reaches it: a vacuous pass on
    /// <c>missing[*]</c> would claim an absence nobody checked.</summary>
    [Test]
    public async Task DoesNotHaveJsonProperty_Wildcard_ThrowsWhereverResolutionWouldStop(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await Assert.That(async () => await Assert.That("{}").DoesNotHaveJsonProperty("missing[*]"))
            .Throws<System.ArgumentException>();
        await Assert.That(async () => await Assert.That("{\"items\":[{\"a\":1}]}").DoesNotHaveJsonProperty("items[*].b"))
            .Throws<System.ArgumentException>();
    }
}
