using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using TUnit.Assertions.Exceptions;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// A value with a line break (a property name, a path, an exception message, a response body, a deserialized value)
/// must not break the line structure of a failure message: a name or path shows a backslash, CR and LF escaped, and
/// quoted raw text shows CR and LF escaped. Text cut at a length limit never ends in half a surrogate pair. Every
/// expected text below spells the escape as a backslash and a letter, so the assertion fails on a raw line break.
/// </summary>
[Category("Smoke")]
[Timeout(5_000)]
internal sealed partial class LineBreakEscapingTests
{
    private static async Task<string> FailureAsync(Func<Task> act)
    {
        var ex = await Assert.That(act).Throws<AssertionException>();
        return ex!.Message;
    }

    private static HttpResponseMessage Response(HttpStatusCode status, string body, string mediaType = "application/json")
    {
        var response = new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8) };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        return response;
    }

    /// <summary>Reports whether <paramref name="text"/> holds half of a surrogate pair.</summary>
    private static bool HasLoneSurrogate(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            if (char.IsHighSurrogate(text[i]) && (i + 1 == text.Length || !char.IsLowSurrogate(text[i + 1])))
            {
                return true;
            }

            if (char.IsLowSurrogate(text[i]) && (i == 0 || !char.IsHighSurrogate(text[i - 1])))
            {
                return true;
            }
        }

        return false;
    }

    [Test]
    public async Task EquivalenceMismatch_NameWithLineFeed_IsEscaped(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var message = await FailureAsync(async () => await Assert.That("""{"a\nb":2}""").IsEquivalentJsonTo("""{"a\nb":1}"""));

        await Assert.That(message).Contains("at 'a\\nb' the value differs");
    }

    [Test]
    public async Task EquivalenceMismatch_NameWithCarriageReturn_IsEscaped(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var message = await FailureAsync(async () => await Assert.That("""{"a\rb":2}""").IsEquivalentJsonTo("""{"a\rb":1}"""));

        await Assert.That(message).Contains("at 'a\\rb' the value differs");
    }

    [Test]
    public async Task EquivalenceMismatch_NameWithBackslash_DiffersFromNameWithLineFeed(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var message = await FailureAsync(async () => await Assert.That("""{"a\\nb":2}""").IsEquivalentJsonTo("""{"a\\nb":1}"""));

        await Assert.That(message).Contains("at 'a\\\\nb' the value differs");
    }

    [Test]
    public async Task ContainsMismatch_NameWithLineFeed_IsEscaped(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var message = await FailureAsync(async () => await Assert.That("{}").ContainsJson("""{"a\nb":1}"""));

        await Assert.That(message).Contains("at 'a\\nb' the property is missing from the actual document");
    }

    [Test]
    public async Task EquivalenceMismatch_DifferenceSidesWithLineBreaks_AreEscaped(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var difference = new JsonDifference("p\nq", JsonDifferenceKind.Value, "e\r\nf", "g\nh");

        var text = JsonFailureMessage.EquivalenceMismatch(difference);

        await Assert.That(text).IsEqualTo("at 'p\\nq' the value differs\n  expected: e\\r\\nf\n  actual:   g\\nh");
    }

    [Test]
    public async Task ParseFailure_MessageWithLineBreaks_IsOnOneLine(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var text = JsonFailureMessage.ParseFailure(new JsonException("first\r\nsecond"));

        await Assert.That(text).IsEqualTo("the value is not valid JSON: first\\r\\nsecond");
    }

    [Test]
    public async Task ExpectedParseFailure_MessageWithLineBreaks_IsOnOneLine(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var text = JsonFailureMessage.ExpectedParseFailure(new JsonException("first\nsecond"));

        await Assert.That(text).IsEqualTo("the expected document is not valid JSON: first\\nsecond");
    }

    [Test]
    public async Task HasJsonProperty_PathWithLineFeed_IsEscapedInTheFailure(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var message = await FailureAsync(async () => await Assert.That("{}").HasJsonProperty("a\nb"));

        await Assert.That(message).Contains("which has no property 'a\\nb'");
    }

    [Test]
    public async Task HasJsonProperty_ResolvedPrefixWithLineFeed_IsEscapedInTheFailure(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var message = await FailureAsync(async () => await Assert.That("""{"a\nb":{}}""").HasJsonProperty("a\nb.c"));

        await Assert.That(message).Contains("resolved as far as 'a\\nb', which has no property 'c'");
    }

    [Test]
    public async Task HasJsonProperties_PathAndPrefixWithLineFeed_AreEscaped(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var message = await FailureAsync(async () => await Assert.That("""{"a\nb":{}}""").HasJsonProperties(["a\nb.c"]));

        await Assert.That(message).Contains("'a\\nb.c': resolved as far as 'a\\nb', which has no property 'c'");
    }

    [Test]
    public async Task GetJsonValue_PathWithLineFeed_IsEscapedInTheExtractionMessages(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var notFound = await Assert.That(() => "{}".GetJsonValue<int>("a\nb")).Throws<JsonExtractionException>();
        var unreadable = await Assert.That(() => """{"a\nb":"s"}""".GetJsonValue<int>("a\nb")).Throws<JsonExtractionException>();

        await Assert.That(notFound!.Message).Contains("No JSON property at path 'a\\nb'");
        await Assert.That(unreadable!.Message).Contains("The value at 'a\\nb' is \"s\", which does not parse as Int32");
    }

    [Test]
    public async Task GetJsonValues_PathWithLineFeed_IsEscapedInEveryLine(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(() => """{"x":[{"k\nz":"s"},{}]}""".GetJsonValues<int>("x[*].k\nz"))
            .Throws<JsonExtractionException>();

        await Assert.That(ex!.Message).IsEqualTo(
            "Could not read 2 of 2 values at 'x[*].k\\nz':\n"
            + "  'x[0].k\\nz': found \"s\", which does not parse as Int32\n"
            + "  'x[1].k\\nz': resolved as far as 'x[1]', which has no property 'k\\nz'");
    }

    [Test]
    public async Task HasJsonValueMatching_WildcardPathWithLineFeed_IsEscaped(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var failed = await FailureAsync(async () =>
            await Assert.That("""{"x":[{"k\nz":1}]}""").HasJsonValueMatching("x[*].k\nz", static _ => false));
        var missing = await FailureAsync(async () =>
            await Assert.That("""{"x":[{}]}""").HasJsonValueMatching("x[*].k\nz", static _ => true));

        await Assert.That(failed).Contains("found 1 at 'x[0].k\\nz'");
        await Assert.That(missing).Contains("'x[0].k\\nz' did not resolve");
    }

    [Test]
    public async Task MatchesValidationProblemDetails_ErrorKeyWithLineFeed_IsEscaped(CancellationToken ct)
    {
        using var response = Response(
            HttpStatusCode.BadRequest, """{"status":400,"errors":{"f\nk":["a"]}}""", "application/problem+json");
        var expected = new Dictionary<string, string[]>(StringComparer.Ordinal) { ["f\nk"] = ["b"] };

        var message = await FailureAsync(async () =>
            await Assert.That(response).MatchesValidationProblemDetails(400, expected, cancellationToken: ct));

        await Assert.That(message).Contains("errors.f\\nk: [\"a\"] (expected [\"b\"])");
    }

    [Test]
    public async Task HasJsonResponse_DeserializationFailureMessageWithLineBreaks_IsOnOneLine(CancellationToken ct)
    {
        using var response = Response(HttpStatusCode.OK, "1");

        var message = await FailureAsync(async () =>
            await Assert.That(response).HasJsonResponse(HttpStatusCode.OK, LineBreakJsonContext.Default.Multi, new Multi(1), ct));

        await Assert.That(message).Contains("the body did not deserialize as Multi: first\\r\\nsecond; body: 1");
    }

    [Test]
    public async Task HasJsonResponse_PrettyPrintedBody_IsOnOneLine(CancellationToken ct)
    {
        using var response = Response(HttpStatusCode.BadRequest, "{\r\n  \"error\": 1\r\n}");

        var message = await FailureAsync(async () =>
            await Assert.That(response).HasJsonResponse(HttpStatusCode.OK, LineBreakJsonContext.Default.Multi, new Multi(1), ct));

        await Assert.That(message).Contains("the status was 400 BadRequest; body: {\\r\\n  \"error\": 1\\r\\n}");
    }

    [Test]
    public async Task HasJsonResponse_DeserializedValueWithLineFeed_IsOnOneLine(CancellationToken ct)
    {
        using var response = Response(HttpStatusCode.OK, "\"a\\nb\"");

        var message = await FailureAsync(async () =>
            await Assert.That(response).HasJsonResponse(HttpStatusCode.OK, LineBreakJsonContext.Default.String, "x", ct));

        await Assert.That(message).Contains("the body deserialized to a\\nb");
    }

    [Test]
    public async Task HasJsonResponse_BodyWithBackslashes_KeepsThemAsWritten(CancellationToken ct)
    {
        using var response = Response(HttpStatusCode.BadRequest, """{"error":"say \"hi\""}""");

        var message = await FailureAsync(async () =>
            await Assert.That(response).HasJsonResponse(HttpStatusCode.OK, LineBreakJsonContext.Default.Multi, new Multi(1), ct));

        await Assert.That(message).Contains("body: {\"error\":\"say \\\"hi\\\"\"}");
    }

    [Test]
    public async Task RoundtripsCleanlyVia_DeserializationFailureMessageWithLineBreaks_IsOnOneLine(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var message = await FailureAsync(async () =>
            await Assert.That(new Multi(1)).RoundtripsCleanlyVia(LineBreakJsonContext.Default.Multi));

        await Assert.That(message).Contains("the first serialization did not deserialize: first\\r\\nsecond\n  first:  1");
    }

    [Test]
    public async Task FoundString_CutInsideASurrogatePair_DoesNotEndInHalfAPair(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var json = "{\"v\":\"" + new string('a', 59) + "\U0001F600tail\"}";

        var message = await FailureAsync(async () => await Assert.That(json).HasJsonValue("v", "x"));

        await Assert.That(message).Contains("found \"" + new string('a', 59) + "...\"");
        await Assert.That(HasLoneSurrogate(message)).IsFalse();
    }

    [Test]
    public async Task FoundString_ExactlyAtTheLimit_IsNotCut(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var json = "{\"v\":\"" + new string('a', 60) + "\"}";

        var message = await FailureAsync(async () => await Assert.That(json).HasJsonValue("v", "x"));

        await Assert.That(message).Contains("found \"" + new string('a', 60) + "\"");
    }

    [Test]
    public async Task ResponseBody_CutInsideASurrogatePair_DoesNotEndInHalfAPair(CancellationToken ct)
    {
        using var response = Response(HttpStatusCode.BadRequest, new string('a', 255) + "\U0001F600x");

        var message = await FailureAsync(async () =>
            await Assert.That(response).HasJsonResponse(HttpStatusCode.OK, LineBreakJsonContext.Default.Multi, new Multi(1), ct));

        await Assert.That(message).Contains("body: " + new string('a', 255) + "...");
        await Assert.That(HasLoneSurrogate(message)).IsFalse();
    }

    [Test]
    public async Task EquivalenceSide_CutInsideASurrogatePair_DoesNotEndInHalfAPair(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var actual = "[\"" + new string('a', 197) + "\U0001F600x\"]";

        var message = await FailureAsync(async () => await Assert.That(actual).IsEquivalentJsonTo("1"));

        await Assert.That(message).Contains("actual:   [\"" + new string('a', 197) + "...");
        await Assert.That(HasLoneSurrogate(message)).IsFalse();
    }

    /// <summary>A type whose converter fails to read with a multi-line message, and writes a number.</summary>
    /// <param name="Id">The identifier.</param>
    [JsonConverter(typeof(MultiConverter))]
    internal sealed record Multi(int Id);

    /// <summary>Reads nothing (it throws a <see cref="JsonException"/> with a multi-line message) and writes the id.</summary>
    internal sealed class MultiConverter : JsonConverter<Multi>
    {
        /// <inheritdoc />
        public override Multi Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => throw new JsonException("first\r\nsecond");

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, Multi value, JsonSerializerOptions options)
            => writer.WriteNumberValue(value.Id);
    }

    /// <summary>The source-generated context of the line-break tests.</summary>
    [JsonSerializable(typeof(Multi))]
    [JsonSerializable(typeof(string))]
    internal sealed partial class LineBreakJsonContext : JsonSerializerContext;
}
