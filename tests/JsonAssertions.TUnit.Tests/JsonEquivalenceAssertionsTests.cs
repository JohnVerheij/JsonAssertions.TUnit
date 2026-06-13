using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TUnit.Assertions.Exceptions;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// End-to-end tests for the <c>[GenerateAssertion]</c>-emitted <c>IsEquivalentJsonTo</c> chains over
/// a JSON <see cref="string"/> and a <see cref="JsonElement"/>. Exercises structural equality
/// (property-order and number-form independence), the <c>IgnoreArrayOrder</c> and <c>IgnorePath</c>
/// options, and each <c>JsonDifference</c> category through the failure-message rendering.
/// </summary>
[Category("Smoke")]
[Timeout(5_000)]
internal sealed class JsonEquivalenceAssertionsTests
{
    [Test]
    public async Task ReorderedProperties_AreEquivalent(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That("""{"b":1,"a":{"y":2,"x":1}}""")
            .IsEquivalentJsonTo("""{"a":{"x":1,"y":2},"b":1}""");
    }

    [Test]
    public async Task NumberForms_AreEquivalent(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // 1, 1.0, and 1e0 are the same numeric value despite different lexical forms.
        await Assert.That("""{"a":1,"b":10,"c":2.5}""")
            .IsEquivalentJsonTo("""{"a":1.0,"b":1e1,"c":2.5}""");
    }

    [Test]
    public async Task LargeNumbers_BeyondDecimal_CompareAsDouble(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // 1e30 exceeds decimal range, so equality falls back to double comparison.
        await Assert.That("""{"n":1e30}""").IsEquivalentJsonTo("""{"n":1.0e30}""");

        var ex = await Assert.That(async () =>
            await Assert.That("""{"n":1e30}""").IsEquivalentJsonTo("""{"n":2e30}"""))
            .Throws<AssertionException>();
        await Assert.That(ex!.Message).Contains("value differs");
    }

    [Test]
    public async Task JsonElementReceiver_IsEquivalent(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"a":[1,2,3]}""");
        await Assert.That(document.RootElement).IsEquivalentJsonTo("""{"a":[1,2,3]}""");
    }

    [Test]
    public async Task ValueMismatch_FailsAtPath(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
            await Assert.That("""{"user":{"name":"alice"}}""").IsEquivalentJsonTo("""{"user":{"name":"bob"}}"""))
            .Throws<AssertionException>();
        await Assert.That(ex!.Message).Contains("\"user.name\": value differs");
        await Assert.That(ex.Message).Contains("expected: \"bob\"");
        await Assert.That(ex.Message).Contains("actual:   \"alice\"");
    }

    [Test]
    public async Task KindMismatch_DumpsContainerRawText(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
            await Assert.That("""{"a":{"x":1}}""").IsEquivalentJsonTo("""{"a":[1,2]}"""))
            .Throws<AssertionException>();
        await Assert.That(ex!.Message).Contains("\"a\": JSON kind differs");
        await Assert.That(ex.Message).Contains("expected: [1,2]");   // array raw text, not "an array"
        await Assert.That(ex.Message).Contains("actual:   {\"x\":1}"); // object raw text, not "an object"
    }

    [Test]
    public async Task BooleanMismatch_IsValueDifference(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
            await Assert.That("""{"ok":false}""").IsEquivalentJsonTo("""{"ok":true}"""))
            .Throws<AssertionException>();
        await Assert.That(ex!.Message).Contains("\"ok\": value differs");
        await Assert.That(ex.Message).Contains("expected: true");
        await Assert.That(ex.Message).Contains("actual:   false");
    }

    [Test]
    public async Task NullValues_AreEquivalent(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That("""{"n":null}""").IsEquivalentJsonTo("""{"n":null}""");
    }

    [Test]
    public async Task NullVersusNumber_IsKindDifference_RendersNull(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
            await Assert.That("""{"n":1}""").IsEquivalentJsonTo("""{"n":null}"""))
            .Throws<AssertionException>();
        await Assert.That(ex!.Message).Contains("\"n\": JSON kind differs");
        await Assert.That(ex.Message).Contains("expected: null");
        await Assert.That(ex.Message).Contains("actual:   1");
    }

    [Test]
    public async Task MissingProperty_FailsNamingPath(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
            await Assert.That("""{"a":1}""").IsEquivalentJsonTo("""{"a":1,"b":2}"""))
            .Throws<AssertionException>();
        await Assert.That(ex!.Message).Contains("\"b\": property missing in the actual document");
        await Assert.That(ex.Message).Contains("actual:   absent");
    }

    [Test]
    public async Task UnexpectedProperty_FailsNamingPath(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
            await Assert.That("""{"a":1,"b":2}""").IsEquivalentJsonTo("""{"a":1}"""))
            .Throws<AssertionException>();
        await Assert.That(ex!.Message).Contains("\"b\": unexpected property in the actual document");
        await Assert.That(ex.Message).Contains("expected: absent");
    }

    [Test]
    public async Task RootMismatch_LabelsRoot(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
            await Assert.That("\"x\"").IsEquivalentJsonTo("\"y\""))
            .Throws<AssertionException>();
        await Assert.That(ex!.Message).Contains("at the root: value differs");
    }

    [Test]
    public async Task ArrayOrder_SensitiveByDefault(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
            await Assert.That("""{"a":[1,2,3]}""").IsEquivalentJsonTo("""{"a":[3,2,1]}"""))
            .Throws<AssertionException>();
        await Assert.That(ex!.Message).Contains("\"a[0]\": value differs");
    }

    [Test]
    public async Task ArrayOrder_IgnoredWhenEnabled(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That("""{"a":[1,2,3]}""")
            .IsEquivalentJsonTo("""{"a":[3,1,2]}""", o => o.IgnoreArrayOrder());
    }

    [Test]
    public async Task ArrayOrderIgnored_UnmatchedElement_Fails(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
            await Assert.That("""[1,2,4]""").IsEquivalentJsonTo("""[1,2,3]""", o => o.IgnoreArrayOrder()))
            .Throws<AssertionException>();
        await Assert.That(ex!.Message).Contains("no equivalent element in the actual array");
    }

    [Test]
    public async Task ArrayLength_Differs_Fails(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
            await Assert.That("""{"a":[1,2]}""").IsEquivalentJsonTo("""{"a":[1,2,3]}"""))
            .Throws<AssertionException>();
        await Assert.That(ex!.Message).Contains("\"a\": array length differs");
        await Assert.That(ex.Message).Contains("expected: 3 element(s)");
        await Assert.That(ex.Message).Contains("actual:   2 element(s)");
    }

    [Test]
    public async Task ArrayLength_Differs_UnorderedAlsoFails(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
            await Assert.That("""[1,2]""").IsEquivalentJsonTo("""[1,2,3]""", o => o.IgnoreArrayOrder()))
            .Throws<AssertionException>();
        await Assert.That(ex!.Message).Contains("array length differs");
    }

    [Test]
    public async Task IgnorePath_ExcludesDifferingValue(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That("""{"id":"a1b2","name":"alice"}""")
            .IsEquivalentJsonTo("""{"id":"zzzz","name":"alice"}""", o => o.IgnorePath("id"));
    }

    [Test]
    public async Task IgnorePath_Wildcard_ExcludesFieldOnEveryElement(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That("""{"items":[{"v":1,"t":"x"},{"v":2,"t":"y"}]}""")
            .IsEquivalentJsonTo(
                """{"items":[{"v":1,"t":"q"},{"v":2,"t":"r"}]}""",
                o => o.IgnorePath("items[*].t"));
    }

    [Test]
    public async Task IgnorePath_ExcludesArrayElementByIndex(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // Ignoring a specific array index skips that whole element, even though it differs.
        await Assert.That("""{"a":[1,99,3]}""")
            .IsEquivalentJsonTo("""{"a":[1,2,3]}""", o => o.IgnorePath("a[1]"));
    }

    [Test]
    public async Task IgnorePath_ExcludesPropertyPresentOnlyInActual(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // The extra "trace" property in actual would normally be an unexpected-property failure.
        await Assert.That("""{"a":1,"trace":"xyz"}""")
            .IsEquivalentJsonTo("""{"a":1}""", o => o.IgnorePath("trace"));
    }

    [Test]
    public async Task LongStringValue_IsTruncatedInMessage(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var longValue = new string('x', 250);
        var ex = await Assert.That(async () =>
            await Assert.That($$"""{"s":"{{longValue}}"}""").IsEquivalentJsonTo("""{"s":"short"}"""))
            .Throws<AssertionException>();
        await Assert.That(ex!.Message).Contains("...");
    }

    [Test]
    public async Task MalformedExpected_FailsWithExpectedParseMessage(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
            await Assert.That("""{"a":1}""").IsEquivalentJsonTo("{not json"))
            .Throws<AssertionException>();
        await Assert.That(ex!.Message).Contains("expected JSON to be parseable");
    }

    [Test]
    public async Task MalformedActual_FailsWithParseMessage(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
            await Assert.That("{not json").IsEquivalentJsonTo("""{"a":1}"""))
            .Throws<AssertionException>();
        await Assert.That(ex!.Message).Contains("parseable JSON");
    }

    [Test]
    public async Task NullExpected_Throws(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(async () =>
            await Assert.That("""{"a":1}""").IsEquivalentJsonTo((string)null!))
            .Throws<ArgumentNullException>();
    }
}
