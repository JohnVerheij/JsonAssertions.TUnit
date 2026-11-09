using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static JsonAssertions.TUnit.Tests.RealUseTestSupport;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// <c>GetJsonValues&lt;T&gt;</c> reads every value a <c>[*]</c> path expands to, in document order, parsed as
/// <c>GetJsonValue&lt;T&gt;</c> parses one; an element that does not resolve or parse is listed, never dropped.
/// </summary>
[Timeout(10_000)]
internal sealed class GetJsonValuesTests
{
    private const string Health = """{"entries":[{"name":"db"},{"name":"cache"},{"name":"queue"}],"empty":[]}""";

    [Test]
    public async Task Wildcard_ReadsEveryValueInDocumentOrder(CancellationToken ct)
    {
        using var document = JsonDocument.Parse(Health);
        using var response = Buffered(Health);

        await Assert.That(Health.GetJsonValues<string>("entries[*].name")).IsEquivalentTo(["db", "cache", "queue"]);
        await Assert.That(document.RootElement.GetJsonValues<string>("entries[*].name")).IsEquivalentTo(["db", "cache", "queue"]);
        await Assert.That(await response.GetJsonValuesAsync<string>("entries[*].name", ct)).IsEquivalentTo(["db", "cache", "queue"]);
    }

    [Test]
    public async Task NestedAndRootWildcards_Expand(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await Assert.That("""{"a":[{"b":[1,2]},{"b":[3]}]}""".GetJsonValues<int>("a[*].b[*]")).IsEquivalentTo([1, 2, 3]);
        await Assert.That("""[{"id":"7"},{"id":8}]""".GetJsonValues<long>("[*].id")).IsEquivalentTo([7L, 8L]);
        await Assert.That("""{"v":[true,"false"]}""".GetJsonValues<bool>("v[*]")).IsEquivalentTo([true, false]);
    }

    /// <summary>A number read as a string is its literal text, as with <c>GetJsonValue&lt;string&gt;</c>.</summary>
    [Test]
    public async Task StringFromANumber_IsTheLiteral(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await Assert.That("""{"v":[1.50,2e3]}""".GetJsonValues<string>("v[*]")).IsEquivalentTo(["1.50", "2e3"]);
    }

    [Test]
    public async Task EmptyArray_GivesAnEmptyList(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await Assert.That(Health.GetJsonValues<string>("empty[*].name")).IsEmpty();
    }

    /// <summary>The design draft's scenario: every failing element by its concrete path.</summary>
    [Test]
    public async Task FailingElements_AreAllListed(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        const string Json = """{"entries":[{"name":"0b3c1d4e-0000-0000-0000-000000000001"},{"name":"0b3c1d4e-0000-0000-0000-000000000002"},{"id":3},{"name":"0b3c1d4e-0000-0000-0000-000000000004"},{"name":7}]}""";

        var ex = await Assert.That(() => Json.GetJsonValues<Guid>("entries[*].name")).Throws<JsonExtractionException>();

        await Assert.That(ex!.Message).IsEqualTo(
            "Could not read 2 of 5 values at 'entries[*].name':\n"
            + "  'entries[2].name': resolved as far as 'entries[2]', which has no property 'name'\n"
            + "  'entries[4].name': found 7, which does not parse as Guid");
    }

    [Test]
    public async Task WildcardOverANonArray_IsOneFailure(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var ex = await Assert.That(() => """{"entries":{"name":"x"}}""".GetJsonValues<string>("entries[*].name"))
            .Throws<JsonExtractionException>();

        await Assert.That(ex!.Message).IsEqualTo(
            "Could not read 1 of 1 value at 'entries[*].name':\n"
            + "  'entries[*]': cannot expand [*]: 'entries' is an Object, not an array");
    }

    [Test]
    public async Task RootFailure_NamesTheFailedSegmentAlone(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var ex = await Assert.That(() => """[{"a":1},{"b":2}]""".GetJsonValues<int>("[*].a"))
            .Throws<JsonExtractionException>();

        await Assert.That(ex!.Message).Contains("  '[1].a': resolved as far as '[1]', which has no property 'a'");
    }

    [Test]
    public async Task PathWithoutAWildcardOrMalformed_Throws(CancellationToken ct)
    {
        using var response = Buffered("not json");

        await Assert.That(() => Health.GetJsonValues<string>("entries[0].name"))
            .Throws<ArgumentException>().WithMessageContaining("has no [*] wildcard, so it names one value; read it with GetJsonValue<T>.", StringComparison.Ordinal);
        await Assert.That(() => "not json".GetJsonValues<string>("entries[*"))
            .Throws<ArgumentException>().WithMessageContaining("unclosed '['", StringComparison.Ordinal);
        await Assert.That(async () => await response.GetJsonValuesAsync<string>("entries.name", ct))
            .Throws<ArgumentException>();
        await Assert.That(() => Health.GetJsonValues<string>(null!)).Throws<ArgumentNullException>();
        await Assert.That(async () => await ((System.Net.Http.HttpResponseMessage)null!).GetJsonValuesAsync<string>("a[*]", ct))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task MalformedBodyOrDefaultElement_ThrowsJsonExtractionException(CancellationToken ct)
    {
        using var response = Buffered("{\"entries\":");

        await Assert.That(() => "{".GetJsonValues<string>("a[*]")).Throws<JsonExtractionException>();
        await Assert.That(async () => await response.GetJsonValuesAsync<string>("entries[*]", ct)).Throws<JsonExtractionException>();
        await Assert.That(() => default(JsonElement).GetJsonValues<string>("a[*]"))
            .Throws<JsonExtractionException>().WithMessageContaining("The JsonElement is default: it holds no JSON value", StringComparison.Ordinal);
    }
}
