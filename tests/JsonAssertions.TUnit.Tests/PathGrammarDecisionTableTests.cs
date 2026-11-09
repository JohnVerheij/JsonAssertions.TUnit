using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TUnit.Assertions.Exceptions;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// The path grammar as a decision table, through <c>HasJsonProperty</c> on one document whose names include the
/// characters a real payload carries: a leading <c>$</c> (System.Text.Json's <c>$type</c> discriminator, <c>$id</c>
/// from reference preservation), spaces, quotes, a backslash, a closing bracket and non-ASCII text. Each row states
/// the 0.7.0 outcome.
/// </summary>
[Timeout(10_000)]
internal sealed class PathGrammarDecisionTableTests
{
    private const string Document = """
        {"$type":"circle","$id":"1","a.b":1,"a":{"b":2},"x":[10,11],"c]d":4,"sp ace":5," lead":6,
         "q\"uote":7,"naïve":8,"名前":9,"emoji😀":10,"a\\b":12,"":13}
        """;

    /// <summary>The outcome of one path against <see cref="Document"/>.</summary>
    internal enum Outcome
    {
        /// <summary>The path resolves.</summary>
        Found,

        /// <summary>The path is well formed and does not resolve: the assertion fails.</summary>
        NotFound,

        /// <summary>The path is malformed: <see cref="ArgumentException"/>.</summary>
        Malformed,
    }

    [Test]
    // Root and leading '$'
    [Arguments("$", Outcome.Found)]
    [Arguments("$.a", Outcome.Found)]
    [Arguments("$.$type", Outcome.Found)]
    [Arguments("$type", Outcome.Found)]
    [Arguments("$id", Outcome.Found)]
    [Arguments("$missing", Outcome.NotFound)]
    [Arguments("$[0]", Outcome.NotFound)]
    [Arguments("$.", Outcome.Malformed)]
    [Arguments("$.[0]", Outcome.Malformed)]
    [Arguments("$..a", Outcome.Malformed)]
    // Names in the first, middle and last position
    [Arguments("a", Outcome.Found)]
    [Arguments("a.b", Outcome.Found)]
    [Arguments("a.missing", Outcome.NotFound)]
    [Arguments("missing.b", Outcome.NotFound)]
    [Arguments("a.b.c", Outcome.NotFound)]
    // Indices in every position
    [Arguments("x[0]", Outcome.Found)]
    [Arguments("x[1]", Outcome.Found)]
    [Arguments("x[01]", Outcome.Found)]
    [Arguments("x[2]", Outcome.NotFound)]
    [Arguments("x[2147483647]", Outcome.NotFound)]
    [Arguments("x[0][0]", Outcome.NotFound)]
    [Arguments("[0]", Outcome.NotFound)]
    [Arguments("a[0]", Outcome.NotFound)]
    // Wildcards (HasJsonProperty expands them)
    [Arguments("x[*]", Outcome.Found)]
    [Arguments("a.b[*]", Outcome.NotFound)]
    [Arguments("[*]", Outcome.NotFound)]
    // Names with awkward characters
    [Arguments("c]d", Outcome.Found)]
    [Arguments("sp ace", Outcome.Found)]
    [Arguments(" lead", Outcome.Found)]
    [Arguments("q\"uote", Outcome.Found)]
    [Arguments("naïve", Outcome.Found)]
    [Arguments("名前", Outcome.Found)]
    [Arguments("emoji😀", Outcome.Found)]
    [Arguments("a\\b", Outcome.Found)]
    // Malformed shapes
    [Arguments(" ", Outcome.Malformed)]
    [Arguments(".a", Outcome.Malformed)]
    [Arguments("a.", Outcome.Malformed)]
    [Arguments("a..b", Outcome.Malformed)]
    [Arguments("missing..b", Outcome.Malformed)]
    [Arguments("x[", Outcome.Malformed)]
    [Arguments("x[]", Outcome.Malformed)]
    [Arguments("x[ 1]", Outcome.Malformed)]
    [Arguments("x[+1]", Outcome.Malformed)]
    [Arguments("x[-1]", Outcome.Malformed)]
    [Arguments("x[99999999999]", Outcome.Malformed)]
    [Arguments("x[0]y", Outcome.Malformed)]
    [Arguments("a.[0]", Outcome.Malformed)]
    public async Task Path_HasTheTableOutcome(string path, Outcome expected, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await Assert.That(await OutcomeOfAsync(path)).IsEqualTo(expected);
    }

    /// <summary>A name containing <c>.</c> or <c>[</c>, or the empty name, has no path: the dot and the bracket are
    /// separators and there is no quoting. The subset assertion reaches such keys, for example the field names of
    /// ASP.NET Core validation errors.</summary>
    [Test]
    public async Task NamesWithADotABracketOrNothing_AreReachedThroughContainsJson(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        const string errors = """{"errors":{"Address.City":["required"],"Items[0].Name":["too long"],"":["empty"]}}""";

        await Assert.That(errors).DoesNotHaveJsonProperty("errors.Address.City");
        await Assert.That(errors).ContainsJson("""{"errors":{"Address.City":["required"],"Items[0].Name":["too long"],"":["empty"]}}""");
    }

    [Test]
    [Arguments("$type")]
    [Arguments("$.$type")]
    public async Task DollarNames_WorkInIgnorePathAndScrubPath(string path, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await Assert.That("""{"$type":"a","v":1}""").IsEquivalentJsonTo("""{"$type":"b","v":1}""", o => o.IgnorePath(path));
        await Assert.That(JsonCanonicalizer.Canonicalize("""{"$type":"a"}""", o => o.ScrubPath(path))).Contains("<scrubbed>");
    }

    [Test]
    public async Task IndexBeyondInt32_SaysTheIndexIsTooLarge(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await Assert.That(async () => await Assert.That(Document).HasJsonProperty("x[99999999999]"))
            .Throws<ArgumentException>().WithMessageContaining("too large", StringComparison.Ordinal);
    }

    [Test]
    public async Task RecursiveDescent_SaysItIsNotSupported(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await Assert.That(async () => await Assert.That(Document).HasJsonProperty("$..a"))
            .Throws<ArgumentException>().WithMessageContaining("recursive descent", StringComparison.Ordinal);
    }

    /// <summary>The documents of System.Text.Json have a default maximum depth of 64: a path down to depth 63
    /// resolves, and a deeper document fails as not valid JSON.</summary>
    [Test]
    public async Task MaximumDepth_PathAtDepth63ResolvesAndDepth65FailsToParse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var atLimit = string.Concat(Enumerable.Repeat("{\"a\":", 63)) + "1" + new string('}', 63);
        var tooDeep = string.Concat(Enumerable.Repeat("{\"a\":", 65)) + "1" + new string('}', 65);

        await Assert.That(atLimit).HasJsonValue(string.Join(".", Enumerable.Repeat("a", 63)), 1);
        await Assert.That(async () => await Assert.That(tooDeep).HasJsonProperty("a"))
            .Throws<AssertionException>().WithMessageContaining("depth of 64", StringComparison.Ordinal);
    }

    /// <summary>A name that occurs twice resolves to its last occurrence, as <see cref="System.Text.Json.JsonElement.TryGetProperty(string, out System.Text.Json.JsonElement)"/>
    /// does.</summary>
    [Test]
    public async Task DuplicateName_ResolvesToItsLastOccurrence(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await Assert.That("""{"a":1,"a":2}""").HasJsonValue("a", 2);
    }

    private static async Task<Outcome> OutcomeOfAsync(string path)
    {
        try
        {
            await Assert.That(Document).HasJsonProperty(path);
            return Outcome.Found;
        }
        catch (AssertionException)
        {
            return Outcome.NotFound;
        }
        catch (ArgumentException)
        {
            return Outcome.Malformed;
        }
    }
}
