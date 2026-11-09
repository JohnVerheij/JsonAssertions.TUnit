using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// The guard for the refactor of <c>JsonEquivalence.CompareElement</c> and <c>CompareObject</c>: 500 seeded document
/// pairs, each compared under five option sets (the last ignores the root, the only path that reaches
/// <c>CompareElement</c>'s own ignore check), with <see cref="JsonEquivalence.Compare(JsonElement, JsonElement, JsonEquivalenceOptions)"/>
/// and <see cref="JsonEquivalence.ContainsAll(JsonElement, JsonElement, JsonEquivalenceOptions)"/>. The embedded baseline
/// holds one hash per pair and option set, and was generated from the 0.6.2 code before the refactor. It must never be
/// regenerated to make this test pass: a difference is a behavior change to find and fix.
/// </summary>
[Timeout(60_000)]
internal sealed class JsonEquivalenceDifferentialTests
{
    private const int PairCount = 500;
    private const ulong Seed = 20261109;
    private const string BaselineResource = "JsonEquivalenceDifferential.baseline.txt";

    private static readonly (string Name, Action<JsonEquivalenceOptions> Configure)[] OptionSets =
    [
        ("default", static _ => { }),
        ("unordered", static o => o.IgnoreArrayOrder()),
        ("ignored", static o => o.IgnorePath("a").IgnorePath("[*].b").IgnorePath("c[*]")),
        ("ignored+unordered", static o => o.IgnorePath("[*].b").IgnorePath("d.e").IgnoreArrayOrder()),
        ("root ignored", static o => o.IgnorePath("$")),
    ];

    private static readonly IReadOnlyList<(string Expected, string Actual)> Pairs = RandomJsonPairs.Generate(Seed, PairCount);

    [Test]
    public async Task CompareAndContainsAll_MatchTheBaselineOfThe062Code(CancellationToken ct)
    {
        var actual = RenderBaseline();
        var baseline = ReadBaseline();
        var mismatches = Mismatches(actual, baseline).ToList();
        var listing = Path.Combine(
            Path.GetTempPath(), "JsonEquivalenceDifferential.actual." + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture) + ".txt");
        if (mismatches.Count > 0)
        {
            await File.WriteAllTextAsync(listing, actual, ct);
        }

        await Assert.That(mismatches).IsEmpty()
            .Because("a refactor of the equivalence engine must not change one result; the full actual listing is in " + listing);
    }

    [Test]
    public async Task EveryDocument_IsEquivalentToAndContainsItself(CancellationToken ct)
    {
        foreach (var (expected, actual) in Pairs)
        {
            ct.ThrowIfCancellationRequested();
            foreach (var json in new[] { expected, actual })
            {
                using var document = JsonDocument.Parse(json);
                foreach (var (name, configure) in OptionSets)
                {
                    var options = Options(configure);
                    await Assert.That(JsonEquivalence.Compare(document.RootElement, document.RootElement, options)).IsNull().Because(name + ": " + json);
                    await Assert.That(JsonEquivalence.ContainsAll(document.RootElement, document.RootElement, options)).IsEmpty().Because(name + ": " + json);
                }
            }
        }
    }

    [Test]
    public async Task Equivalence_IsSymmetricInItsOutcome(CancellationToken ct)
    {
        foreach (var (expected, actual) in Pairs)
        {
            ct.ThrowIfCancellationRequested();
            using var left = JsonDocument.Parse(expected);
            using var right = JsonDocument.Parse(actual);
            foreach (var (name, configure) in OptionSets)
            {
                var forward = JsonEquivalence.Compare(left.RootElement, right.RootElement, Options(configure)) is null;
                var backward = JsonEquivalence.Compare(right.RootElement, left.RootElement, Options(configure)) is null;
                await Assert.That(backward).IsEqualTo(forward).Because(name + ": " + expected + " vs " + actual);
            }
        }
    }

    private static JsonEquivalenceOptions Options(Action<JsonEquivalenceOptions> configure)
    {
        var options = new JsonEquivalenceOptions();
        configure(options);
        return options;
    }

    private static string RenderBaseline()
    {
        var sb = new StringBuilder();
        sb.Append("# JsonEquivalence guard: seed ").Append(Seed.ToString(CultureInfo.InvariantCulture))
            .Append(", ").Append(PairCount.ToString(CultureInfo.InvariantCulture)).Append(" pairs; one line per pair and option set: index, set, SHA-256 prefix of the results\n");
        for (var set = 0; set < OptionSets.Length; set++)
        {
            var equal = Enumerable.Range(0, PairCount).Count(i => Result(i, set).StartsWith("compare: equal\n", StringComparison.Ordinal));
            sb.Append("# set ").Append(set.ToString(CultureInfo.InvariantCulture)).Append(' ').Append(OptionSets[set].Name)
                .Append(": ").Append(equal.ToString(CultureInfo.InvariantCulture)).Append(" equivalent pairs\n");
        }

        for (var i = 0; i < PairCount; i++)
        {
            for (var set = 0; set < OptionSets.Length; set++)
            {
                var hash = SHA256.HashData(Encoding.UTF8.GetBytes(Result(i, set)));
                sb.Append(i.ToString("D3", CultureInfo.InvariantCulture)).Append(' ').Append(set.ToString(CultureInfo.InvariantCulture))
                    .Append(' ').Append(Convert.ToHexString(hash, 0, 4)).Append('\n');
            }
        }

        return sb.ToString();
    }

    private static string Result(int index, int set)
    {
        var (expected, actual) = Pairs[index];
        using var expectedDocument = JsonDocument.Parse(expected);
        using var actualDocument = JsonDocument.Parse(actual);
        var options = Options(OptionSets[set].Configure);
        var sb = new StringBuilder("compare: ");
        Describe(sb, JsonEquivalence.Compare(expectedDocument.RootElement, actualDocument.RootElement, options));
        var all = JsonEquivalence.ContainsAll(expectedDocument.RootElement, actualDocument.RootElement, options);
        sb.Append("contains: ").Append(all.Count.ToString(CultureInfo.InvariantCulture)).Append('\n');
        foreach (var difference in all)
        {
            Describe(sb, difference);
        }

        return sb.ToString();
    }

    private static void Describe(StringBuilder sb, JsonDifference? difference)
    {
        if (difference is null)
        {
            sb.Append("equal\n");
            return;
        }

        sb.Append(difference.Path).Append('|').Append(difference.Kind).Append('|')
            .Append(difference.Expected).Append('|').Append(difference.Actual).Append('\n');
    }

    private static IEnumerable<string> Mismatches(string actual, string baseline)
    {
        var expectedLines = baseline.Split('\n').Where(l => l.Length > 0 && l[0] != '#').ToArray();
        var actualLines = actual.Split('\n').Where(l => l.Length > 0 && l[0] != '#').ToArray();
        if (expectedLines.Length != actualLines.Length)
        {
            yield return "the baseline has " + expectedLines.Length.ToString(CultureInfo.InvariantCulture) + " result lines, the run "
                + actualLines.Length.ToString(CultureInfo.InvariantCulture);
            yield break;
        }

        var reported = 0;
        for (var i = 0; i < actualLines.Length && reported < 5; i++)
        {
            if (!string.Equals(actualLines[i], expectedLines[i], StringComparison.Ordinal))
            {
                reported++;
                var index = i / OptionSets.Length;
                var set = i % OptionSets.Length;
                yield return "pair " + index.ToString(CultureInfo.InvariantCulture) + " under '" + OptionSets[set].Name + "': "
                    + Pairs[index].Expected + " vs " + Pairs[index].Actual + " now gives " + Result(index, set).Replace('\n', ' ');
            }
        }
    }

    private static string ReadBaseline()
    {
        using var stream = typeof(JsonEquivalenceDifferentialTests).Assembly.GetManifestResourceStream(BaselineResource);
        if (stream is null)
        {
            return string.Empty;
        }

        using var reader = new StreamReader(stream, Encoding.UTF8);
        return reader.ReadToEnd().Replace("\r\n", "\n", StringComparison.Ordinal);
    }
}
