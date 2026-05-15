using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TUnit.Assertions.Exceptions;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// End-to-end tests for the <c>[GenerateAssertion]</c>-emitted value-at-path chains:
/// <c>HasJsonValue</c> for <see cref="string"/>, <see cref="bool"/>, and numeric expected
/// values over a JSON <see cref="string"/> and a <see cref="JsonElement"/>. The failing cases
/// inspect the exception message so both <c>JsonFailureMessage.ValueMismatch</c> branches
/// (path resolved but value differs; path did not resolve) and the found-element rendering
/// are exercised end-to-end.
/// </summary>
[Category("Smoke")]
[Timeout(5_000)]
internal sealed class JsonValueAssertionsTests
{
    private const string SampleJson =
        """{"user":{"name":"alice","age":30,"active":true,"tags":["x"],"meta":{},"note":null}}""";

    [Test]
    public async Task HasJsonValue_String_Match_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(SampleJson).HasJsonValue("user.name", "alice");
    }

    [Test]
    public async Task HasJsonValue_String_Match_JsonElement_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse(SampleJson);

        await Assert.That(document.RootElement).HasJsonValue("user.name", "alice");
    }

    [Test]
    public async Task HasJsonValue_String_DifferentValue_FailsShowingFound(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That(SampleJson).HasJsonValue("user.name", "bob");
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("to have JSON value \"bob\" at path \"user.name\"");
        await Assert.That(ex.Message).Contains("found: \"alice\"");
    }

    [Test]
    public async Task HasJsonValue_String_PathNotFound_FailsWithFailurePoint(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That(SampleJson).HasJsonValue("user.email", "x@y.z");
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("resolved as far as: user");
        await Assert.That(ex.Message).Contains("no property \"email\"");
    }

    [Test]
    public async Task HasJsonValue_Boolean_Match_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(SampleJson).HasJsonValue("user.active", true);
    }

    [Test]
    public async Task HasJsonValue_Boolean_Match_JsonElement_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse(SampleJson);

        await Assert.That(document.RootElement).HasJsonValue("user.active", true);
    }

    [Test]
    public async Task HasJsonValue_Boolean_DifferentValue_FailsShowingFound(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That(SampleJson).HasJsonValue("user.active", false);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("to have JSON value false at path \"user.active\"");
        await Assert.That(ex.Message).Contains("found: true");
    }

    [Test]
    public async Task HasJsonValue_Number_Match_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(SampleJson).HasJsonValue("user.age", 30);
    }

    [Test]
    public async Task HasJsonValue_Number_Match_JsonElement_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse(SampleJson);

        await Assert.That(document.RootElement).HasJsonValue("user.age", 30);
    }

    [Test]
    public async Task HasJsonValue_Number_DifferentValue_FailsShowingFound(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That(SampleJson).HasJsonValue("user.age", 25);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("to have JSON value 25 at path \"user.age\"");
        await Assert.That(ex.Message).Contains("found: 30");
    }

    [Test]
    public async Task HasJsonValue_Number_KindMismatchAgainstString_FailsShowingFoundString(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That(SampleJson).HasJsonValue("user.name", 30);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("found: \"alice\"");
    }

    [Test]
    public async Task HasJsonValue_KindMismatchAgainstObject_FailsRenderingObjectByKind(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That(SampleJson).HasJsonValue("user.meta", "x");
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("found: an object");
    }

    [Test]
    public async Task HasJsonValue_KindMismatchAgainstArray_FailsRenderingArrayByKind(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That(SampleJson).HasJsonValue("user.tags", "x");
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("found: an array");
    }

    [Test]
    public async Task HasJsonValue_KindMismatchAgainstNull_FailsRenderingNull(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That(SampleJson).HasJsonValue("user.note", "x");
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("found: null");
    }

    [Test]
    public async Task HasJsonValueMatching_PredicateSucceeds_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(SampleJson).HasJsonValueMatching("user.age", static e => e.GetInt32() >= 18);
    }

    [Test]
    public async Task HasJsonValueMatching_PredicateFails_FailsWithExpectedDescription(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That(SampleJson).HasJsonValueMatching("user.age", static e => e.GetInt32() < 18);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("to have JSON value a value matching the predicate at path \"user.age\"");
    }

    [Test]
    public async Task HasJsonValueOneOf_StringMatches_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That("""{"status":"Degraded"}""")
            .HasJsonValueOneOf("status", ["Healthy", "Degraded", "Unhealthy"]);
    }

    [Test]
    public async Task HasJsonValueOneOf_StringNotInCandidates_FailsShowingCandidatesAndFound(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That("""{"status":"Critical"}""")
                .HasJsonValueOneOf("status", ["Healthy", "Degraded", "Unhealthy"]);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("to have JSON value one of { \"Healthy\", \"Degraded\", \"Unhealthy\" } at path \"status\"");
        await Assert.That(ex.Message).Contains("found: \"Critical\"");
    }

    [Test]
    public async Task HasJsonValueOneOf_NumericMatches_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That("""{"code":503}""").HasJsonValueOneOf("code", [200d, 503d, 504d]);
    }

    [Test]
    public async Task HasJsonValueParsableAs_Guid_StringParses_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That("""{"id":"550e8400-e29b-41d4-a716-446655440000"}""")
            .HasJsonValueParsableAs<Guid>("id");
    }

    [Test]
    public async Task HasJsonValueParsableAs_DateTimeOffset_StringParses_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That("""{"at":"2026-05-15T08:30:00+02:00"}""").HasJsonValueParsableAs<DateTimeOffset>("at");
    }

    [Test]
    public async Task HasJsonValueParsableAs_Guid_StringMalformed_Fails(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That("""{"id":"not-a-guid"}""").HasJsonValueParsableAs<Guid>("id");
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("a JSON string parseable as Guid");
        await Assert.That(ex.Message).Contains("found: \"not-a-guid\"");
    }

    [Test]
    public async Task HasJsonValueParsableAs_NonStringElement_Fails(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That("""{"id":42}""").HasJsonValueParsableAs<Guid>("id");
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("a JSON string parseable as Guid");
    }

    [Test]
    public async Task HasJsonValueMatching_JsonElement_PredicateSucceeds_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse(SampleJson);

        await Assert.That(document.RootElement).HasJsonValueMatching("user.age", static e => e.GetInt32() >= 18);
    }

    [Test]
    public async Task HasJsonValueOneOf_JsonElement_StringMatches_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"status":"Degraded"}""");

        await Assert.That(document.RootElement)
            .HasJsonValueOneOf("status", ["Healthy", "Degraded", "Unhealthy"]);
    }

    [Test]
    public async Task HasJsonValueOneOf_JsonElement_NumericMatches_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"code":503}""");

        await Assert.That(document.RootElement).HasJsonValueOneOf("code", [200d, 503d, 504d]);
    }

    [Test]
    public async Task HasJsonValueParsableAs_JsonElement_Guid_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"id":"550e8400-e29b-41d4-a716-446655440000"}""");

        await Assert.That(document.RootElement).HasJsonValueParsableAs<Guid>("id");
    }

    [Test]
    public async Task HasJsonValueMatching_NullPredicate_String_ThrowsArgumentNullException(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(() => JsonValueAssertions.HasJsonValueMatching(SampleJson, "user.age", null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task HasJsonValueMatching_NullPredicate_JsonElement_ThrowsArgumentNullException(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse(SampleJson);
        var element = document.RootElement;
        await Assert.That(() => JsonValueAssertions.HasJsonValueMatching(element, "user.age", null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task HasJsonValueOneOf_NullStringCandidates_String_ThrowsArgumentNullException(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(() => JsonValueAssertions.HasJsonValueOneOf(SampleJson, "user.name", (string[])null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task HasJsonValueOneOf_NullNumericCandidates_String_ThrowsArgumentNullException(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(() => JsonValueAssertions.HasJsonValueOneOf(SampleJson, "user.age", (double[])null!))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task HasJsonValueOneOf_StringWithQuoteAndBackslash_RendersEscapedCandidates(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That("""{"x":"hello"}""")
                .HasJsonValueOneOf("x", ["he\"llo", "back\\slash"]);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("\"he\\\"llo\"");
        await Assert.That(ex.Message).Contains("\"back\\\\slash\"");
    }

    [Test]
    public async Task HasJsonValueOneOf_StringWithControlCharacters_RendersUnicodeEscape(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That("""{"x":"y"}""")
                .HasJsonValueOneOf("x", ["a\nb", "c\tdef", "e" + ((char)1).ToString() + "f", "g\bh", "i\fj", "k\rl"]);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("\\n");
        await Assert.That(ex.Message).Contains("\\t");
        await Assert.That(ex.Message).Contains("\\u0001");
        await Assert.That(ex.Message).Contains("\\b");
        await Assert.That(ex.Message).Contains("\\f");
        await Assert.That(ex.Message).Contains("\\r");
    }

    [Test]
    public async Task HasJsonValueMatching_PathDoesNotResolve_FailsWithFailurePoint(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That(SampleJson).HasJsonValueMatching("user.missing", static _ => true);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("resolved as far as: user");
    }

    [Test]
    public async Task HasJsonValueOneOf_String_PathDoesNotResolve_FailsWithFailurePoint(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That(SampleJson).HasJsonValueOneOf("user.missing", ["a"]);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("resolved as far as: user");
    }

    [Test]
    public async Task HasJsonValueOneOf_Numeric_PathDoesNotResolve_FailsWithFailurePoint(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That(SampleJson).HasJsonValueOneOf("user.missing", [1d]);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("resolved as far as: user");
    }

    [Test]
    public async Task HasJsonValueParsableAs_PathDoesNotResolve_FailsWithFailurePoint(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That(SampleJson).HasJsonValueParsableAs<Guid>("user.missing");
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("resolved as far as: user");
    }
}
