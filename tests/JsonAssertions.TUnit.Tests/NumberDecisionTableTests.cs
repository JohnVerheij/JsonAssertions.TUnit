using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using TUnit.Assertions.Exceptions;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// Numbers as a decision table: each integer overload at its boundaries, <c>-0</c>, exponents and fractions, the
/// numeric-string forms, <c>double</c> beyond its range, and the equivalence comparison with literals that
/// <see cref="decimal"/> would round. Each row states the 0.7.0 outcome.
/// </summary>
[Timeout(30_000)]
internal sealed class NumberDecisionTableTests
{
    [Test]
    // int
    [Arguments("2147483647", "int", "2147483647", true)]
    [Arguments("-2147483648", "int", "-2147483648", true)]
    [Arguments("2147483648", "int", "0", false)]
    [Arguments("\"2147483647\"", "int", "2147483647", true)]
    [Arguments("-0", "int", "0", true)]
    [Arguments("\"-0\"", "int", "0", true)]
    [Arguments("1e2", "int", "100", false)]
    [Arguments("100.0", "int", "100", false)]
    [Arguments("\" 42 \"", "int", "42", false)]
    [Arguments("\"+42\"", "int", "42", false)]
    [Arguments("\"042\"", "int", "42", false)]
    [Arguments("\"-042\"", "int", "-42", false)]
    [Arguments("\"0\"", "int", "0", true)]
    [Arguments("\"\"", "int", "0", false)]
    [Arguments("\"-\"", "int", "0", false)]
    [Arguments("\"4a\"", "int", "4", false)]
    // uint
    [Arguments("4294967295", "uint", "4294967295", true)]
    [Arguments("-1", "uint", "0", false)]
    [Arguments("-0", "uint", "0", true)]
    [Arguments("\"-0\"", "uint", "0", true)]
    // long
    [Arguments("9223372036854775807", "long", "9223372036854775807", true)]
    [Arguments("\"-9223372036854775808\"", "long", "-9223372036854775808", true)]
    [Arguments("-0", "long", "0", true)]
    [Arguments("\"-0\"", "long", "0", true)]
    [Arguments("\"+1\"", "long", "1", false)]
    // ulong
    [Arguments("\"18446744073709551615\"", "ulong", "18446744073709551615", true)]
    [Arguments("18446744073709551616", "ulong", "0", false)]
    [Arguments("-0", "ulong", "0", true)]
    [Arguments("\"-0\"", "ulong", "0", true)]
    public async Task IntegerOverload_HasTheTableOutcome(string literal, string type, string expected, bool matches, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var json = "{\"v\":" + literal + "}";

        await Assert.That(await MatchesAsync(json, type, expected)).IsEqualTo(matches);
    }

    [Test]
    [Arguments("1e2", 100.0, true)]
    [Arguments("-0", 0.0, true)]
    [Arguments("0.1", 0.1, true)]
    [Arguments("1e308", 1e308, true)]
    [Arguments("\"42\"", 42.0, false)]
    [Arguments("1e400", double.MaxValue, false)]
    public async Task DoubleOverload_HasTheTableOutcome(string literal, double expected, bool matches, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var json = "{\"v\":" + literal + "}";

        await Assert.That(await PassesAsync(async () => await Assert.That(json).HasJsonValue("v", expected))).IsEqualTo(matches);
    }

    /// <summary>JSON has no NaN or infinity, so a non-finite expectation can't be meant: it throws at every
    /// <see cref="double"/> entry point, before the JSON is parsed or the body read. On 0.6.2,
    /// <c>HasJsonValue("v", double.PositiveInfinity)</c> passed for <c>1e400</c>.</summary>
    [Test]
    [Arguments(double.NaN)]
    [Arguments(double.PositiveInfinity)]
    [Arguments(double.NegativeInfinity)]
    public async Task DoubleOverload_NonFiniteExpectation_Throws(double expected, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        const string Json = "{\"v\":1e400}";
        using var document = System.Text.Json.JsonDocument.Parse(Json);
        var element = document.RootElement;
        using var response = RealUseTestSupport.Buffered(Json);

        await Assert.That(async () => await Assert.That("not json").HasJsonValue("v", expected)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(async () => await Assert.That(element).HasJsonValue("v", expected)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(async () => await Assert.That(response).HasJsonValue("v", expected, ct)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(async () => await Assert.That("not json").HasJsonValueOneOf("v", [1.0, expected])).Throws<ArgumentOutOfRangeException>();
        await Assert.That(async () => await Assert.That(element).HasJsonValueOneOf("v", [expected])).Throws<ArgumentOutOfRangeException>();
        await Assert.That(async () => await Assert.That(response).HasJsonValueOneOf("v", [expected], ct)).Throws<ArgumentOutOfRangeException>();
    }

    /// <summary>A literal beyond <see cref="double"/> range reads as an infinity, which the core comparison never
    /// matches.</summary>
    [Test]
    public async Task DoubleComparison_OutOfRangeLiteral_NeverMatches(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = System.Text.Json.JsonDocument.Parse("[1e400,-1e400]");
        var positive = document.RootElement[0];
        var negative = document.RootElement[1];

        await Assert.That(JsonValueComparison.Matches(positive, double.PositiveInfinity)).IsFalse();
        await Assert.That(JsonValueComparison.Matches(negative, double.NegativeInfinity)).IsFalse();
        await Assert.That(JsonValueComparison.MatchesAny(positive, double.PositiveInfinity, double.NegativeInfinity)).IsFalse();
    }

    /// <summary><c>GetJsonValue&lt;T&gt;</c> and <c>HasJsonValueParsableAs&lt;T&gt;</c> are generic over
    /// <see cref="IParsable{TSelf}"/>, so they parse by the type's own rules, where the integer overloads take only a
    /// JSON integer literal.</summary>
    [Test]
    public async Task ParsableInteger_FollowsTheTypesOwnRules(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        const string Json = """{"v":"+42"}""";

        await Assert.That(Json.GetJsonValue<int>("v")).IsEqualTo(42);
        await Assert.That(Json).HasJsonValueParsableAs<int>("v");
        await Assert.That(await PassesAsync(async () => await Assert.That(Json).HasJsonValue("v", 42))).IsFalse();
    }

    /// <summary>The one-of overloads read integers the same way as the single-value ones.</summary>
    [Test]
    public async Task IntegerOneOf_ReadsLikeTheSingleValueOverload(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await Assert.That("{\"v\":-0}").HasJsonValueOneOf("v", [0U, 1U]);
        await Assert.That("{\"v\":-0}").HasJsonValueOneOf("v", [0UL, 1UL]);
        await Assert.That("{\"v\":\"-0\"}").HasJsonValueOneOf("v", [0, 1]);
        await Assert.That(await PassesAsync(async () => await Assert.That("{\"v\":\"+1\"}").HasJsonValueOneOf("v", [1L, 2L]))).IsFalse();
        await Assert.That(await PassesAsync(async () => await Assert.That("{\"v\":\"01\"}").HasJsonValueOneOf("v", [1U]))).IsFalse();
    }

    /// <summary>The integer one-of overloads say when the string they found is not a JSON integer literal, as the
    /// single-value overloads do; a path that does not resolve names where it stopped.</summary>
    [Test]
    public async Task IntegerOneOf_SaysWhenAStringIsNotAnIntegerLiteral(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var signed = await Assert.That(async () => await Assert.That("{\"v\":\"+42\"}").HasJsonValueOneOf("v", [42, 43]))
            .Throws<AssertionException>();
        var wrong = await Assert.That(async () => await Assert.That("{\"v\":\"7\"}").HasJsonValueOneOf("v", [42UL]))
            .Throws<AssertionException>();
        var missing = await Assert.That(async () => await Assert.That("{}").HasJsonValueOneOf("v", [42L]))
            .Throws<AssertionException>();

        await Assert.That(signed!.Message).Contains("but found \"+42\", which is not a JSON integer literal (candidates: 42, 43)\n");
        await Assert.That(wrong!.Message).Contains("but found \"7\" (candidates: 42)\n");
        await Assert.That(missing!.Message).Contains("but resolved as far as the root, which has no property 'v'\n");
    }

    [Test]
    [Arguments("1E+2", "100", true)]
    [Arguments("0.1", "1e-1", true)]
    [Arguments("1.10", "1.1", true)]
    [Arguments("-0", "0", true)]
    [Arguments("-0.0e5", "0", true)]
    [Arguments("79228162514264337593543950336", "79228162514264337593543950336.0", true)]
    [Arguments("1e-30", "0", false)]
    [Arguments("1.00000000000000000000000000001", "1", false)]
    [Arguments("0.1000000000000000000000000000001", "0.1", false)]
    [Arguments("1e99999999999", "10e99999999998", true)]
    [Arguments("1e99999999999", "1e99999999998", false)]
    public async Task Equivalence_ComparesNumbersExactly(string actual, string expected, bool equivalent, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var actualJson = "{\"v\":" + actual + "}";
        var expectedJson = "{\"v\":" + expected + "}";

        await Assert.That(await PassesAsync(async () => await Assert.That(actualJson).IsEquivalentJsonTo(expectedJson))).IsEqualTo(equivalent);
        await Assert.That(await PassesAsync(async () => await Assert.That(actualJson).ContainsJson(expectedJson))).IsEqualTo(equivalent);
    }

    /// <summary>A hostile literal must not hang the comparison: on 0.6.2 the trailing-zero loop is quadratic in the
    /// literal's length.</summary>
    [Test]
    public async Task Equivalence_LongRunOfZeros_ComparesQuickly(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var actualJson = "{\"v\":1" + new string('0', 100_000) + "}";
        var stopwatch = Stopwatch.StartNew();

        await Assert.That(actualJson).IsEquivalentJsonTo("{\"v\":1e100000}");

        await Assert.That(stopwatch.Elapsed).IsLessThan(TimeSpan.FromSeconds(1));
    }

    private static Task<bool> MatchesAsync(string json, string type, string expected) => type switch
    {
        "int" => PassesAsync(async () => await Assert.That(json).HasJsonValue("v", int.Parse(expected, System.Globalization.CultureInfo.InvariantCulture))),
        "uint" => PassesAsync(async () => await Assert.That(json).HasJsonValue("v", uint.Parse(expected, System.Globalization.CultureInfo.InvariantCulture))),
        "long" => PassesAsync(async () => await Assert.That(json).HasJsonValue("v", long.Parse(expected, System.Globalization.CultureInfo.InvariantCulture))),
        _ => PassesAsync(async () => await Assert.That(json).HasJsonValue("v", ulong.Parse(expected, System.Globalization.CultureInfo.InvariantCulture))),
    };

    private static async Task<bool> PassesAsync(Func<Task> assertion)
    {
        try
        {
            await assertion();
            return true;
        }
        catch (AssertionException)
        {
            return false;
        }
    }
}
