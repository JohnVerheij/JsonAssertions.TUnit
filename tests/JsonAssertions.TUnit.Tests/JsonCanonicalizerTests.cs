using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JsonAssertions;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// Tests for <see cref="JsonCanonicalizer.Canonicalize(string, Action{JsonCanonicalizeOptions})"/>:
/// deterministic sorted-key / stable-indent / LF output, value preservation (including unknown
/// fields), JSON-path scrubbing (literal and wildcard paths), the custom scrub token, and argument
/// validation.
/// </summary>
[Category("Smoke")]
[Timeout(5_000)]
internal sealed class JsonCanonicalizerTests
{
    [Test]
    public async Task Canonicalize_SortsKeys_StableIndent_Lf_ExactShape(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var result = JsonCanonicalizer.Canonicalize("""{"b":1,"a":2}""");
        await Assert.That(result).IsEqualTo("{\n  \"a\": 2,\n  \"b\": 1\n}");
    }

    [Test]
    public async Task Canonicalize_IsOrderInsensitive(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var a = JsonCanonicalizer.Canonicalize("""{"b":1,"a":2,"c":3}""");
        var b = JsonCanonicalizer.Canonicalize("""{"c":3,"a":2,"b":1}""");
        await Assert.That(a).IsEqualTo(b);
    }

    [Test]
    public async Task Canonicalize_PreservesUnknownFieldsAndPrimitives(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var result = JsonCanonicalizer.Canonicalize("""{"known":1,"unexpected":true,"z":null,"s":"x"}""");

        await Assert.That(result).Contains("\"unexpected\": true");
        await Assert.That(result).Contains("\"z\": null");
        await Assert.That(result).Contains("\"s\": \"x\"");
    }

    [Test]
    public async Task Canonicalize_ScrubLiteralPath_ReplacesValue(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var result = JsonCanonicalizer.Canonicalize(
            """{"a":{"secret":"xyz"}}""",
            o => o.ScrubPath("a.secret"));

        await Assert.That(result).Contains("<scrubbed>");
        await Assert.That(result).DoesNotContain("xyz");
    }

    [Test]
    public async Task Canonicalize_ScrubWildcardPath_ReplacesEveryElement(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var result = JsonCanonicalizer.Canonicalize(
            """[{"token":"t1"},{"token":"t2"}]""",
            o => o.ScrubPath("[*].token"));

        await Assert.That(result).DoesNotContain("t1");
        await Assert.That(result).DoesNotContain("t2");
        var first = result.IndexOf("<scrubbed>", StringComparison.Ordinal);
        await Assert.That(first).IsGreaterThanOrEqualTo(0);
        await Assert.That(result.IndexOf("<scrubbed>", first + 1, StringComparison.Ordinal)).IsGreaterThan(first);
    }

    [Test]
    public async Task Canonicalize_CustomScrubToken(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var result = JsonCanonicalizer.Canonicalize(
            """{"k":"v"}""",
            o => o.ScrubPath("k").WithScrubToken("MASKED"));

        await Assert.That(result).Contains("MASKED");
        await Assert.That(result).DoesNotContain("\"v\"");
    }

    [Test]
    public async Task Canonicalize_ScrubPathThatDoesNotResolve_IsNoOp(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var result = JsonCanonicalizer.Canonicalize(
            """{"present":"value"}""",
            o => o.ScrubPath("absent.path"));

        await Assert.That(result).Contains("\"value\"");
        await Assert.That(result).DoesNotContain("<scrubbed>");
    }

    [Test]
    public async Task Canonicalize_NullJson_Throws(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(() => JsonCanonicalizer.Canonicalize(null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Canonicalize_InvalidJson_Throws(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(() => JsonCanonicalizer.Canonicalize("{not json")).Throws<JsonException>();
    }

    [Test]
    public async Task ScrubPath_NullOrWhitespace_Throws(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(() => JsonCanonicalizer.Canonicalize("{}", o => o.ScrubPath(" ")))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task WithScrubToken_Null_Throws(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(() => JsonCanonicalizer.Canonicalize("{}", o => o.WithScrubToken(null!)))
            .Throws<ArgumentNullException>();
    }
}
