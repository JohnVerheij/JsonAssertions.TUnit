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
    public async Task LargeNumbers_BeyondDecimal_CompareCanonically(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // 1e30 exceeds decimal range, so equality uses the canonical form (not a lossy double compare).
        await Assert.That("""{"n":1e30}""").IsEquivalentJsonTo("""{"n":1.0e30}""");

        var ex = await Assert.That(async () =>
            await Assert.That("""{"n":1e30}""").IsEquivalentJsonTo("""{"n":2e30}"""))
            .Throws<AssertionException>();
        await Assert.That(ex!.Message).Contains("value differs");

        // Precision collision: 1e29 and 1e29 + 1 are distinct integers that round to the same double,
        // so a double compare would wrongly equate them. The canonical form keeps them distinct.
        var big = $$"""{"n":{{"1" + new string('0', 29)}}}""";          // 10^29
        var bigPlusOne = $$"""{"n":{{"1" + new string('0', 28) + "1"}}}"""; // 10^29 + 1
        var collision = await Assert.That(async () => await Assert.That(big).IsEquivalentJsonTo(bigPlusOne))
            .Throws<AssertionException>();
        await Assert.That(collision!.Message).Contains("value differs");
    }

    [Test]
    public async Task BeyondDoubleRange_EqualLiterals_AreEquivalent(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // 1e400 exceeds double range; equality is decided by a canonical numeric form, not lexically.
        await Assert.That("""{"n":1e400}""").IsEquivalentJsonTo("""{"n":1e400}""");
    }

    [Test]
    public async Task BeyondDoubleRange_FormVariants_AreEquivalent(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // Number-form independence holds in the overflow range: exponent case, scientific-form
        // normalization, a fractional mantissa, and a written-out integer all denote the same value.
        await Assert.That("""{"n":1e400}""").IsEquivalentJsonTo("""{"n":1E400}""");
        await Assert.That("""{"n":1e400}""").IsEquivalentJsonTo("""{"n":10e399}""");
        await Assert.That("""{"a":1.5e400}""").IsEquivalentJsonTo("""{"a":15e399}""");
        await Assert.That($$"""{"n":{{"1" + new string('0', 400)}}}""").IsEquivalentJsonTo("""{"n":1e400}""");
    }

    [Test]
    public async Task BeyondDoubleRange_DistinctLiterals_AreNotEquivalent(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // Both read as +Infinity as doubles, but they are distinct numbers and must not be equated.
        var ex = await Assert.That(async () =>
            await Assert.That("""{"n":1e400}""").IsEquivalentJsonTo("""{"n":2e400}"""))
            .Throws<AssertionException>();
        await Assert.That(ex!.Message).Contains("value differs");
    }

    [Test]
    public async Task BeyondDoubleRange_OppositeSigns_AreNotEquivalent(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That("""{"n":-1e400}""").IsEquivalentJsonTo("""{"n":-1e400}""");
        await Assert.That(async () =>
            await Assert.That("""{"n":1e400}""").IsEquivalentJsonTo("""{"n":-1e400}"""))
            .Throws<AssertionException>();
    }

    [Test]
    [Timeout(5_000)]
    public async Task ZeroAgainstBeyondDoubleRange_TerminatesAndIsNotEquivalent(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // Zero reaches canonicalization only when the other operand is non-finite as a double (1e400
        // reads as +Infinity, so the double path is skipped). The zero guard keeps the trailing-zero
        // loop terminating; the values are distinct, so they must not be equated. Without the guard
        // this assertion would hang and trip the timeout.
        var ex = await Assert.That(async () =>
            await Assert.That("""{"n":0}""").IsEquivalentJsonTo("""{"n":1e400}"""))
            .Throws<AssertionException>();
        await Assert.That(ex!.Message).Contains("value differs");
    }

    [Test]
    public async Task BeyondDoubleRange_ExponentOverflowingInt_FallsBackToRawText(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // An exponent that overflows Int32 cannot canonicalize, so equality falls back to raw text:
        // identical literals still match, and differing ones do not.
        await Assert.That("""{"n":1e9999999999}""").IsEquivalentJsonTo("""{"n":1e9999999999}""");
        await Assert.That(async () =>
            await Assert.That("""{"n":1e9999999999}""").IsEquivalentJsonTo("""{"n":2e9999999999}"""))
            .Throws<AssertionException>();
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
    public async Task IgnorePath_Root_IgnoresEntireDocument(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // Ignoring the root "$" excludes the whole document, so any two documents compare equivalent.
        await Assert.That("""{"a":1}""").IsEquivalentJsonTo("""{"b":2,"c":[3]}""", o => o.IgnorePath("$"));
    }

    [Test]
    public async Task IgnorePath_ExcludedElement_SuppressesLengthDifference(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // The only differing node (index 0) is explicitly excluded, so the length difference it would
        // otherwise cause is suppressed, in both directions.
        await Assert.That("""[1]""").IsEquivalentJsonTo("""[]""", o => o.IgnorePath("[0]"));
        await Assert.That("""[]""").IsEquivalentJsonTo("""[1]""", o => o.IgnorePath("[0]"));
    }

    [Test]
    public async Task IgnorePath_WildcardArrayElements_SuppressesExtraAndMissing(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // a[*] ignores every element of a, so differing element counts no longer matter.
        await Assert.That("""{"a":[1,2,3]}""").IsEquivalentJsonTo("""{"a":[1]}""", o => o.IgnorePath("a[*]"));
        await Assert.That("""{"a":[]}""").IsEquivalentJsonTo("""{"a":[9,9,9]}""", o => o.IgnorePath("a[*]"));
        // Unordered with the same wildcard also collapses to an empty comparison.
        await Assert.That("""{"a":[3,2,1]}""")
            .IsEquivalentJsonTo("""{"a":[1]}""", o => o.IgnoreArrayOrder().IgnorePath("a[*]"));
    }

    [Test]
    public async Task IgnorePath_ExcludedElement_StillFailsWhenNonIgnoredCountsDiffer(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // Excluding index 0 must not hide a genuine extra element among the non-ignored ones.
        var ex = await Assert.That(async () =>
            await Assert.That("""[9,1,2]""").IsEquivalentJsonTo("""[9,1]""", o => o.IgnorePath("[0]")))
            .Throws<AssertionException>();
        await Assert.That(ex!.Message).Contains("array length differs");
        await Assert.That(ex.Message).Contains("expected: 1 element(s)");
        await Assert.That(ex.Message).Contains("actual:   2 element(s)");
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
