using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TUnit.Assertions.Exceptions;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// Drives the kind-dependent rendering arms of <c>JsonFailureMessage</c> end-to-end: the
/// <c>DescribeKind</c> branch reached by a negative-existence failure for each JSON value
/// kind, and the <c>RenderElement</c> branch reached by a value-mismatch failure whose found
/// element is each kind. These are the switch arms the happy-path behavior tests do not
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

        await Assert.That(ex!.Message).Contains("but found an Object");
    }

    [Test]
    public async Task DoesNotHaveJsonProperty_FoundNumber_NamesNumberKind(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That(KindsJson).DoesNotHaveJsonProperty("num");
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("but found 5");
    }

    [Test]
    public async Task DoesNotHaveJsonProperty_FoundBoolean_NamesBooleanKind(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That(KindsJson).DoesNotHaveJsonProperty("flag");
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("but found true");
    }

    [Test]
    public async Task DoesNotHaveJsonProperty_FoundNull_NamesNullKind(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That(KindsJson).DoesNotHaveJsonProperty("nothing");
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("but found null");
    }

    [Test]
    public async Task DoesNotHaveJsonProperty_FoundArray_NamesArrayKind(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That(KindsJson).DoesNotHaveJsonProperty("list");
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("but found an Array");
    }

    /// <summary>The assertions refuse a default element before resolving, so a default element's resolution reaches
    /// the factories only through the public API: the fallback arm of the kind description.</summary>
    [Test]
    public async Task PropertyNotFound_ForADefaultElementResolution_NamesUndefinedKind(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var message = JsonFailureMessage.PropertyNotFound("anything", JsonPath.Resolve(default, "anything"));

        await Assert.That(message).Contains("an undefined value");
    }

    /// <summary>The fallback arm of the found-element rendering, reached the same way.</summary>
    [Test]
    public async Task ValueMismatch_ForADefaultElementFoundAtTheRoot_RendersUndefined(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var message = JsonFailureMessage.ValueMismatch("$", JsonPath.Resolve(default, "$"), "1");

        await Assert.That(message).IsEqualTo("found an undefined value");
    }

    [Test]
    public async Task HasJsonValue_FoundFalseBoolean_RendersFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That(KindsJson).HasJsonValue("flagOff", "x");
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("but found false");
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

    [Test]
    public async Task HasJsonProperty_IndexOutOfRangeOnArray_RendersNoElementAtIndex(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That("""{"items":[1,2]}""").HasJsonProperty("items[5]");
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("but resolved as far as 'items', which has 2 elements, so no index [5]");
    }

    [Test]
    public async Task HasJsonProperty_IndexOnNonArray_RendersCannotIndex(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That("""{"user":{"name":"a"}}""").HasJsonProperty("user[0]");
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("but resolved as far as 'user', which is an Object, not an array");
    }

    [Test]
    public async Task HasJsonProperty_IndexOutOfRangeAtRoot_RendersRootInLocation(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That("""[1,2,3]""").HasJsonProperty("[5]");
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("but resolved as far as the root, which has 3 elements, so no index [5]");
    }

    /// <summary>A path that runs into a scalar names the scalar's kind, Boolean and Null included.</summary>
    [Test]
    public async Task HasJsonProperty_ThroughABooleanOrANull_NamesTheKind(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var throughBoolean = await Assert.That(async () => await Assert.That(KindsJson).HasJsonProperty("flag.x"))
            .Throws<AssertionException>();
        var throughNull = await Assert.That(async () => await Assert.That(KindsJson).HasJsonProperty("nothing[0]"))
            .Throws<AssertionException>();

        await Assert.That(throughBoolean!.Message).Contains("but resolved as far as 'flag', which is a Boolean, not an object");
        await Assert.That(throughNull!.Message).Contains("but resolved as far as 'nothing', which is a Null, not an array");
    }

    [Test]
    public async Task HasJsonArrayLength_OnAScalar_NamesTheKind(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var onNull = await Assert.That(async () => await Assert.That(KindsJson).HasJsonArrayLength("nothing", 1))
            .Throws<AssertionException>();
        var onBoolean = await Assert.That(async () => await Assert.That(KindsJson).HasJsonArrayLength("flagOff", 1))
            .Throws<AssertionException>();

        await Assert.That(onNull!.Message).Contains("but found a Null\n");
        await Assert.That(onBoolean!.Message).Contains("but found a Boolean\n");
    }
}
