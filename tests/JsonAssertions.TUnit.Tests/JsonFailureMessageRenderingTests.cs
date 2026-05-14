using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TUnit.Assertions.Exceptions;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// Drives the kind-dependent rendering arms of <c>JsonFailureMessage</c> end-to-end: the
/// <c>DescribeKind</c> branch reached by a negative-existence failure for each JSON value
/// kind, and the <c>RenderElement</c> branch reached by a value-mismatch failure whose found
/// element is each kind. These are the switch arms the happy-path behaviour tests do not
/// otherwise reach.
/// </summary>
[Category("Smoke")]
[Timeout(5_000)]
internal sealed class JsonFailureMessageRenderingTests
{
    private const string KindsJson =
        """{"obj":{},"num":5,"flag":true,"flagOff":false,"nothing":null,"text":"hi","list":[1]}""";

    [Test]
    public async Task DoesNotHaveJsonProperty_FoundObject_NamesObjectKind(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That(KindsJson).DoesNotHaveJsonProperty("obj");
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("but an Object exists at that path");
    }

    [Test]
    public async Task DoesNotHaveJsonProperty_FoundNumber_NamesNumberKind(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That(KindsJson).DoesNotHaveJsonProperty("num");
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("but a Number exists at that path");
    }

    [Test]
    public async Task DoesNotHaveJsonProperty_FoundBoolean_NamesBooleanKind(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That(KindsJson).DoesNotHaveJsonProperty("flag");
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("but a Boolean exists at that path");
    }

    [Test]
    public async Task DoesNotHaveJsonProperty_FoundNull_NamesNullKind(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That(KindsJson).DoesNotHaveJsonProperty("nothing");
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("but a Null exists at that path");
    }

    [Test]
    public async Task DoesNotHaveJsonProperty_FoundArray_NamesArrayKind(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That(KindsJson).DoesNotHaveJsonProperty("list");
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("but an Array exists at that path");
    }

    [Test]
    public async Task HasJsonProperty_OnDefaultElement_NamesUndefinedKind(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // A default JsonElement has ValueKind Undefined; this is the only way to reach the
        // fallback arm of the kind-description switch.
        var ex = await Assert.That(async () =>
        {
            await Assert.That(default(JsonElement)).HasJsonProperty("anything");
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("an undefined value");
    }

    [Test]
    public async Task HasJsonValue_FoundFalseBoolean_RendersFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That(KindsJson).HasJsonValue("flagOff", "x");
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("found: false");
    }

    [Test]
    public async Task HasJsonValue_LongStringValue_IsTruncatedInMessage(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var longValue = new string('a', 200);
        var json = $$"""{"text":"{{longValue}}"}""";

        var ex = await Assert.That(async () =>
        {
            await Assert.That(json).HasJsonValue("text", "expected");
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("...");
    }
}
