using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JsonAssertions;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// Tests for the framework-agnostic <see cref="JsonValueComparison"/> core: each <c>Matches</c>
/// overload against a matching value, a value mismatch of the same kind, and a kind mismatch
/// (which must return <see langword="false"/> rather than throw). The integer overloads accept
/// either a JSON number or a numeric JSON string, so both encodings are exercised.
/// </summary>
[Category("Smoke")]
[Timeout(5_000)]
internal sealed class JsonValueComparisonTests
{
    private static JsonElement Parse(string json, string property)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty(property).Clone();
    }

    [Test]
    public async Task Matches_String_EqualValue_ReturnsTrue(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":"alice"}""", "v"), "alice")).IsTrue();
    }

    [Test]
    public async Task Matches_String_DifferentValue_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":"alice"}""", "v"), "bob")).IsFalse();
    }

    [Test]
    public async Task Matches_String_KindMismatch_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":42}""", "v"), "42")).IsFalse();
    }

    [Test]
    public async Task Matches_Boolean_EqualValue_ReturnsTrue(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":true}""", "v"), true)).IsTrue();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":false}""", "v"), false)).IsTrue();
    }

    [Test]
    public async Task Matches_Boolean_DifferentValue_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":true}""", "v"), false)).IsFalse();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":false}""", "v"), true)).IsFalse();
    }

    [Test]
    public async Task Matches_Boolean_KindMismatch_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":"true"}""", "v"), true)).IsFalse();
    }

    [Test]
    public async Task Matches_Number_EqualValue_ReturnsTrue(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":30}""", "v"), 30d)).IsTrue();
    }

    [Test]
    public async Task Matches_Number_DifferentValue_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":30}""", "v"), 25d)).IsFalse();
    }

    [Test]
    public async Task Matches_Number_KindMismatch_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        // The double overload is JSON-number-only (unlike the integer overloads), so a
        // string-encoded number does not match it.
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":"30"}""", "v"), 30d)).IsFalse();
    }

    [Test]
    public async Task MatchesAny_String_OneCandidateMatches_ReturnsTrue(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.MatchesAny(Parse("""{"v":"b"}""", "v"), "a", "b", "c")).IsTrue();
    }

    [Test]
    public async Task MatchesAny_String_NoMatch_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.MatchesAny(Parse("""{"v":"z"}""", "v"), "a", "b", "c")).IsFalse();
    }

    [Test]
    public async Task MatchesAny_String_KindMismatch_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.MatchesAny(Parse("""{"v":42}""", "v"), "42")).IsFalse();
    }

    [Test]
    public async Task MatchesAny_String_NullCandidates_Throws(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var element = Parse("""{"v":"a"}""", "v");
        await Assert.That(() => JsonValueComparison.MatchesAny(element, (string[])null!)).Throws<System.ArgumentNullException>();
    }

    [Test]
    public async Task MatchesAny_Number_OneCandidateMatches_ReturnsTrue(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.MatchesAny(Parse("""{"v":503}""", "v"), 200d, 503d)).IsTrue();
    }

    [Test]
    public async Task MatchesAny_Number_NoMatch_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.MatchesAny(Parse("""{"v":418}""", "v"), 200d, 503d)).IsFalse();
    }

    [Test]
    public async Task MatchesAny_Number_KindMismatch_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.MatchesAny(Parse("""{"v":"503"}""", "v"), 503d)).IsFalse();
    }

    [Test]
    public async Task MatchesAny_Number_NullCandidates_Throws(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var element = Parse("""{"v":1}""", "v");
        await Assert.That(() => JsonValueComparison.MatchesAny(element, (double[])null!)).Throws<System.ArgumentNullException>();
    }

    // --- Int32: number-encoded passing cases ---

    [Test]
    public async Task Matches_Int32_NumberEqual_ReturnsTrue(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":30}""", "v"), 30)).IsTrue();
    }

    [Test]
    public async Task Matches_Int32_NegativeNumberEqual_ReturnsTrue(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":-2147483648}""", "v"), int.MinValue)).IsTrue();
    }

    // --- Int32: string-encoded passing cases ---

    [Test]
    public async Task Matches_Int32_StringEncodedEqual_ReturnsTrue(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":"30"}""", "v"), 30)).IsTrue();
    }

    [Test]
    public async Task Matches_Int32_NegativeStringEncodedEqual_ReturnsTrue(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":"-2147483648"}""", "v"), int.MinValue)).IsTrue();
    }

    // --- Int32: failure paths ---

    [Test]
    public async Task Matches_Int32_DifferentNumber_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":30}""", "v"), 31)).IsFalse();
    }

    [Test]
    public async Task Matches_Int32_DifferentString_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":"30"}""", "v"), 31)).IsFalse();
    }

    [Test]
    public async Task Matches_Int32_NonNumericString_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":"thirty"}""", "v"), 30)).IsFalse();
    }

    [Test]
    public async Task Matches_Int32_FractionalNumber_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":30.5}""", "v"), 30)).IsFalse();
    }

    [Test]
    public async Task Matches_Int32_OutOfRangeNumber_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":2147483648}""", "v"), 1)).IsFalse();
    }

    [Test]
    public async Task Matches_Int32_OutOfRangeString_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":"2147483648"}""", "v"), 1)).IsFalse();
    }

    [Test]
    public async Task Matches_Int32_OtherKind_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":true}""", "v"), 1)).IsFalse();
    }

    // --- UInt32: number- and string-encoded passing cases + failure paths ---

    [Test]
    public async Task Matches_UInt32_NumberEqual_ReturnsTrue(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":4294967295}""", "v"), uint.MaxValue)).IsTrue();
    }

    [Test]
    public async Task Matches_UInt32_StringEncodedEqual_ReturnsTrue(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":"4294967295"}""", "v"), uint.MaxValue)).IsTrue();
    }

    [Test]
    public async Task Matches_UInt32_NegativeNumber_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":-1}""", "v"), 1U)).IsFalse();
    }

    [Test]
    public async Task Matches_UInt32_NegativeString_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":"-1"}""", "v"), 1U)).IsFalse();
    }

    [Test]
    public async Task Matches_UInt32_NonNumericString_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":"five"}""", "v"), 5U)).IsFalse();
    }

    [Test]
    public async Task Matches_UInt32_OtherKind_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":null}""", "v"), 5U)).IsFalse();
    }

    // --- Int64: number-encoded (System.Text.Json) and string-encoded (protobuf) passing cases ---

    [Test]
    public async Task Matches_Int64_NumberEncodedEqual_ReturnsTrue(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":123456789012345}""", "v"), 123456789012345L)).IsTrue();
    }

    [Test]
    public async Task Matches_Int64_StringEncodedEqual_ReturnsTrue(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":"123456789012345"}""", "v"), 123456789012345L)).IsTrue();
    }

    [Test]
    public async Task Matches_Int64_NegativeStringEncodedEqual_ReturnsTrue(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":"-9223372036854775808"}""", "v"), long.MinValue)).IsTrue();
    }

    // --- Int64: failure paths ---

    [Test]
    public async Task Matches_Int64_DifferentNumber_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":123}""", "v"), 124L)).IsFalse();
    }

    [Test]
    public async Task Matches_Int64_DifferentString_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":"123"}""", "v"), 124L)).IsFalse();
    }

    [Test]
    public async Task Matches_Int64_FractionalNumber_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":123.5}""", "v"), 123L)).IsFalse();
    }

    [Test]
    public async Task Matches_Int64_UnparsableString_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":"not-a-number"}""", "v"), 1L)).IsFalse();
    }

    [Test]
    public async Task Matches_Int64_OtherKind_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":false}""", "v"), 1L)).IsFalse();
    }

    // --- UInt64: number- and string-encoded passing cases + failure paths ---

    [Test]
    public async Task Matches_UInt64_NumberEncodedEqual_ReturnsTrue(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":18446744073709551615}""", "v"), ulong.MaxValue)).IsTrue();
    }

    [Test]
    public async Task Matches_UInt64_StringEncodedEqual_ReturnsTrue(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":"18446744073709551615"}""", "v"), ulong.MaxValue)).IsTrue();
    }

    [Test]
    public async Task Matches_UInt64_NegativeNumber_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":-1}""", "v"), 1UL)).IsFalse();
    }

    [Test]
    public async Task Matches_UInt64_NegativeString_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":"-1"}""", "v"), 1UL)).IsFalse();
    }

    [Test]
    public async Task Matches_UInt64_UnparsableString_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":"x"}""", "v"), 1UL)).IsFalse();
    }

    [Test]
    public async Task Matches_UInt64_OtherKind_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.Matches(Parse("""{"v":[]}""", "v"), 1UL)).IsFalse();
    }

    // --- MatchesAny Int32: both encodings + failure paths ---

    [Test]
    public async Task MatchesAny_Int32_NumberCandidateMatches_ReturnsTrue(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.MatchesAny(Parse("""{"v":200}""", "v"), 100, 200)).IsTrue();
    }

    [Test]
    public async Task MatchesAny_Int32_StringCandidateMatches_ReturnsTrue(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.MatchesAny(Parse("""{"v":"200"}""", "v"), 100, 200)).IsTrue();
    }

    [Test]
    public async Task MatchesAny_Int32_NoMatch_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.MatchesAny(Parse("""{"v":300}""", "v"), 100, 200)).IsFalse();
    }

    [Test]
    public async Task MatchesAny_Int32_NonNumericString_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.MatchesAny(Parse("""{"v":"x"}""", "v"), 200)).IsFalse();
    }

    [Test]
    public async Task MatchesAny_Int32_OtherKind_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.MatchesAny(Parse("""{"v":true}""", "v"), 200)).IsFalse();
    }

    [Test]
    public async Task MatchesAny_Int32_NullCandidates_Throws(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var element = Parse("""{"v":1}""", "v");
        await Assert.That(() => JsonValueComparison.MatchesAny(element, (int[])null!)).Throws<System.ArgumentNullException>();
    }

    // --- MatchesAny UInt32: both encodings + failure paths ---

    [Test]
    public async Task MatchesAny_UInt32_NumberCandidateMatches_ReturnsTrue(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.MatchesAny(Parse("""{"v":4294967295}""", "v"), 1U, uint.MaxValue)).IsTrue();
    }

    [Test]
    public async Task MatchesAny_UInt32_StringCandidateMatches_ReturnsTrue(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.MatchesAny(Parse("""{"v":"4294967295"}""", "v"), 1U, uint.MaxValue)).IsTrue();
    }

    [Test]
    public async Task MatchesAny_UInt32_NoMatch_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.MatchesAny(Parse("""{"v":5}""", "v"), 1U, 2U)).IsFalse();
    }

    [Test]
    public async Task MatchesAny_UInt32_NegativeNumber_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.MatchesAny(Parse("""{"v":-5}""", "v"), 5U)).IsFalse();
    }

    [Test]
    public async Task MatchesAny_UInt32_NonNumericString_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.MatchesAny(Parse("""{"v":"five"}""", "v"), 5U)).IsFalse();
    }

    [Test]
    public async Task MatchesAny_UInt32_OtherKind_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.MatchesAny(Parse("""{"v":null}""", "v"), 5U)).IsFalse();
    }

    [Test]
    public async Task MatchesAny_UInt32_NullCandidates_Throws(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var element = Parse("""{"v":1}""", "v");
        await Assert.That(() => JsonValueComparison.MatchesAny(element, (uint[])null!)).Throws<System.ArgumentNullException>();
    }

    // --- MatchesAny Int64: both encodings + failure paths ---

    [Test]
    public async Task MatchesAny_Int64_NumberCandidateMatches_ReturnsTrue(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.MatchesAny(Parse("""{"v":200}""", "v"), 100L, 200L)).IsTrue();
    }

    [Test]
    public async Task MatchesAny_Int64_StringCandidateMatches_ReturnsTrue(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.MatchesAny(Parse("""{"v":"200"}""", "v"), 100L, 200L)).IsTrue();
    }

    [Test]
    public async Task MatchesAny_Int64_NoMatch_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.MatchesAny(Parse("""{"v":"300"}""", "v"), 100L, 200L)).IsFalse();
    }

    [Test]
    public async Task MatchesAny_Int64_NonNumericString_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.MatchesAny(Parse("""{"v":"x"}""", "v"), 200L)).IsFalse();
    }

    [Test]
    public async Task MatchesAny_Int64_OtherKind_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.MatchesAny(Parse("""{"v":false}""", "v"), 200L)).IsFalse();
    }

    [Test]
    public async Task MatchesAny_Int64_NullCandidates_Throws(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var element = Parse("""{"v":"1"}""", "v");
        await Assert.That(() => JsonValueComparison.MatchesAny(element, (long[])null!)).Throws<System.ArgumentNullException>();
    }

    // --- MatchesAny UInt64: both encodings + failure paths ---

    [Test]
    public async Task MatchesAny_UInt64_NumberCandidateMatches_ReturnsTrue(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.MatchesAny(Parse("""{"v":18446744073709551615}""", "v"), 1UL, ulong.MaxValue)).IsTrue();
    }

    [Test]
    public async Task MatchesAny_UInt64_StringCandidateMatches_ReturnsTrue(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.MatchesAny(Parse("""{"v":"18446744073709551615"}""", "v"), 1UL, ulong.MaxValue)).IsTrue();
    }

    [Test]
    public async Task MatchesAny_UInt64_NoMatch_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.MatchesAny(Parse("""{"v":"5"}""", "v"), 1UL, 2UL)).IsFalse();
    }

    [Test]
    public async Task MatchesAny_UInt64_NegativeNumber_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.MatchesAny(Parse("""{"v":-5}""", "v"), 5UL)).IsFalse();
    }

    [Test]
    public async Task MatchesAny_UInt64_UnparsableString_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.MatchesAny(Parse("""{"v":"x"}""", "v"), 1UL)).IsFalse();
    }

    [Test]
    public async Task MatchesAny_UInt64_OtherKind_ReturnsFalse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(JsonValueComparison.MatchesAny(Parse("""{"v":[]}""", "v"), 1UL)).IsFalse();
    }

    [Test]
    public async Task MatchesAny_UInt64_NullCandidates_Throws(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var element = Parse("""{"v":"1"}""", "v");
        await Assert.That(() => JsonValueComparison.MatchesAny(element, (ulong[])null!)).Throws<System.ArgumentNullException>();
    }
}
