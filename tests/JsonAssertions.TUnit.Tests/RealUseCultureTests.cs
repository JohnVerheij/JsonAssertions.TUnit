using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using TUnit.Assertions.Exceptions;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// The failure texts under <c>nl-NL</c> (a decimal comma), <c>tr-TR</c> (the dotted and dotless i) and the invariant
/// culture: a message must read the same in each. The full-message pins of the message rule run under these three
/// cultures too.
/// </summary>
[Timeout(10_000)]
internal sealed class RealUseCultureTests
{
    private static readonly string[] Cultures = ["", "nl-NL", "tr-TR"];

    [Test]
    public async Task IntegerStringAndSubsetFailures_ReadTheSameInEveryCulture(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await AssertSameInEveryCultureAsync(async () => await Assert.That("""{"v":1}""").HasJsonValue("v", -5));
        await AssertSameInEveryCultureAsync(async () => await Assert.That("""{"I":"x"}""").HasJsonValue("I", "y"));
        await AssertSameInEveryCultureAsync(async () => await Assert.That("""{"v":1.25}""").ContainsJson("""{"v":2.5}"""));
        await AssertSameInEveryCultureAsync(async () => await Assert.That("""{"I":1}""").HasJsonValueKind("I", System.Text.Json.JsonValueKind.String));
    }

    /// <summary>On 0.6.2 the generated expectation line renders the expected <see cref="double"/> in the current
    /// culture (<c>2,5</c> under nl-NL and tr-TR); the message rule's generic wording removes it.</summary>
    [Test]
    public async Task DoubleFailures_ReadTheSameInEveryCulture(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await AssertSameInEveryCultureAsync(async () => await Assert.That("""{"v":1.25}""").HasJsonValue("v", 2.5));
        await AssertSameInEveryCultureAsync(async () => await Assert.That("""{"v":1.25}""").HasJsonValueOneOf("v", [2.5, -3.5]));
    }

    /// <summary>.NET behavior, pinned for the README: <see cref="IParsable{TSelf}"/> parses a <see cref="double"/>
    /// with thousands separators allowed, so the invariant culture reads the JSON string <c>"1,5"</c> as 15.</summary>
    [Test]
    public async Task ParsableDouble_ReadsACommaAsAThousandsSeparator(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await Assert.That("""{"v":"1,5"}""".GetJsonValue<double>("v")).IsEqualTo(15.0);
    }

    private static async Task AssertSameInEveryCultureAsync(Func<Task> failingAssertion)
    {
        var messages = new string[Cultures.Length];
        var original = CultureInfo.CurrentCulture;
        try
        {
            for (var i = 0; i < Cultures.Length; i++)
            {
                CultureInfo.CurrentCulture = Cultures[i].Length is 0 ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo(Cultures[i]);
                try
                {
                    await failingAssertion();
                    messages[i] = "(passed)";
                }
                catch (AssertionException ex)
                {
                    messages[i] = ex.Message;
                }
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }

        await Assert.That(messages[1]).IsEqualTo(messages[0]);
        await Assert.That(messages[2]).IsEqualTo(messages[0]);
    }
}
