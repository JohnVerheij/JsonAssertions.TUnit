using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// Tests for the typed extraction helpers (`GetJsonValue&lt;T&gt;` / `GetJsonString` /
/// `GetJsonElement`) over a JSON <see cref="string"/>, a <see cref="JsonElement"/>, and an
/// <see cref="HttpResponseMessage"/>. Exercises number/string/bool/Guid/DateTimeOffset parsing,
/// number-or-numeric-string tolerance, nested and array-index paths, detached-element survival
/// after the source document is disposed, culture invariance, and the path-context failure modes.
/// </summary>
[Category("Smoke")]
[Timeout(5_000)]
internal sealed class JsonExtractionTests
{
    private static HttpResponseMessage JsonResponse(string body)
        => new() { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    [Test]
    public async Task GetJsonValue_Int_FromNumber(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That("""{"cycleId":42}""".GetJsonValue<int>("cycleId")).IsEqualTo(42);
    }

    [Test]
    public async Task GetJsonValue_Int_FromNumericString(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That("""{"cycleId":"42"}""".GetJsonValue<int>("cycleId")).IsEqualTo(42);
    }

    [Test]
    public async Task GetJsonValue_Long_Bool_Guid_DateTimeOffset(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var guid = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var json = $$"""{"big":9000000000,"ok":true,"id":"{{guid}}","when":"2026-07-11T01:02:03+00:00"}""";

        await Assert.That(json.GetJsonValue<long>("big")).IsEqualTo(9000000000L);
        await Assert.That(json.GetJsonValue<bool>("ok")).IsTrue();
        await Assert.That(json.GetJsonValue<Guid>("id")).IsEqualTo(guid);
        await Assert.That(json.GetJsonValue<DateTimeOffset>("when"))
            .IsEqualTo(new DateTimeOffset(2026, 7, 11, 1, 2, 3, TimeSpan.Zero));
    }

    [Test]
    public async Task GetJsonValue_NestedAndArrayIndexPath(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var json = """{"cycle":{"pickPlans":[{"rank":7},{"rank":9}]}}""";
        await Assert.That(json.GetJsonValue<int>("cycle.pickPlans[1].rank")).IsEqualTo(9);
    }

    [Test]
    public async Task GetJsonValue_IsCultureInvariant(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("nl-NL");
            await Assert.That("""{"ratio":2.5}""".GetJsonValue<double>("ratio")).IsEqualTo(2.5);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Test]
    public async Task GetJsonValue_JsonElementReceiver(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"count":3}""");
        await Assert.That(document.RootElement.GetJsonValue<int>("count")).IsEqualTo(3);
    }

    [Test]
    public async Task GetJsonString_ReturnsStringValue(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That("""{"name":"alpha"}""".GetJsonString("name")).IsEqualTo("alpha");
    }

    [Test]
    public async Task GetJsonString_NonStringValue_Throws(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = Assert.Throws<JsonExtractionException>(() => """{"n":5}""".GetJsonString("n"));
        await Assert.That(ex!.Message).Contains("a JSON string", StringComparison.Ordinal);
    }

    [Test]
    public async Task GetJsonElement_ReturnsDetachedSubtree_SurvivesDispose(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // The returned element must stay valid after the parsed document is gone.
        var objects = """{"objects":[{"id":1},{"id":2},{"id":3}]}""".GetJsonElement("objects");
        await Assert.That(objects.ValueKind).IsEqualTo(JsonValueKind.Array);
        await Assert.That(objects.GetArrayLength()).IsEqualTo(3);
    }

    [Test]
    public async Task GetJsonElement_FromElement_Subtree(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse("""{"user":{"id":7,"name":"x"}}""");
        var user = document.RootElement.GetJsonElement("user");
        await Assert.That(user.GetJsonValue<int>("id")).IsEqualTo(7);
    }

    [Test]
    public async Task GetJsonValueAsync_HttpResponse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var response = JsonResponse("""{"cycleId":42}""");
        await Assert.That(await response.GetJsonValueAsync<int>("cycleId", ct)).IsEqualTo(42);
    }

    [Test]
    public async Task GetJsonStringAsync_And_GetJsonElementAsync_HttpResponse(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var response = JsonResponse("""{"status":"Healthy","entries":[{"name":"db"}]}""");
        await Assert.That(await response.GetJsonStringAsync("status", ct)).IsEqualTo("Healthy");
        var entries = await response.GetJsonElementAsync("entries", ct);
        await Assert.That(entries.GetArrayLength()).IsEqualTo(1);
    }

    [Test]
    public async Task Get_PathNotFound_ThrowsWithPathContext(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = Assert.Throws<JsonExtractionException>(() => """{"a":1}""".GetJsonValue<int>("a.b.c"));
        await Assert.That(ex!.Message).Contains("path", StringComparison.Ordinal);
        await Assert.That(ex.Message).Contains("resolved as far as", StringComparison.Ordinal);
    }

    [Test]
    public async Task GetJsonValue_Unparseable_Throws(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = Assert.Throws<JsonExtractionException>(() => """{"id":"not-a-guid"}""".GetJsonValue<Guid>("id"));
        await Assert.That(ex!.Message).Contains("Guid", StringComparison.Ordinal);
    }

    [Test]
    public async Task GetJsonValue_OnNonScalar_Throws(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // An object/array/null value has no scalar text to parse as T.
        var onObject = Assert.Throws<JsonExtractionException>(() => """{"a":{"b":1}}""".GetJsonValue<int>("a"));
        await Assert.That(onObject!.Message).Contains("Int32", StringComparison.Ordinal);
        Assert.Throws<JsonExtractionException>(() => """{"a":[1,2]}""".GetJsonValue<int>("a"));
        Assert.Throws<JsonExtractionException>(() => """{"a":null}""".GetJsonValue<int>("a"));
    }

    [Test]
    public async Task Get_MalformedJson_ThrowsExplained(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = Assert.Throws<JsonExtractionException>(() => "{ not json".GetJsonValue<int>("a"));
        await Assert.That(ex!.Message).Contains("parseable JSON", StringComparison.Ordinal);
        await Assert.That(ex.InnerException).IsTypeOf<JsonException>();
    }

    [Test]
    public async Task Get_NullArguments_Throw(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(() => ((string)null!).GetJsonValue<int>("a")).Throws<ArgumentNullException>();
        await Assert.That(() => "{}".GetJsonValue<int>(null!)).Throws<ArgumentNullException>();
        using var response = JsonResponse("{}");
        await Assert.That(async () => await response.GetJsonValueAsync<int>(null!, ct)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task JsonExtractionException_HasStandardConstructors(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(new JsonExtractionException().Message).IsNotNull();
        await Assert.That(new JsonExtractionException("m").Message).IsEqualTo("m");
        var inner = new InvalidOperationException("i");
        await Assert.That(new JsonExtractionException("m", inner).InnerException).IsSameReferenceAs(inner);
    }
}
