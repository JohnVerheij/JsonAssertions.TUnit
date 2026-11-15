using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// A property name that appears twice in one object: the last occurrence is the property on both sides of a
/// comparison (so a document is equivalent to itself), and the canonical form keeps the occurrences in document order.
/// A <see langword="default"/> <see cref="JsonElement"/> holds no JSON value, so the core primitives refuse it.
/// </summary>
[Category("Smoke")]
[Timeout(5_000)]
internal sealed class DuplicateNamesAndDefaultElementTests
{
    private static JsonDocument Parse(string json) => JsonDocument.Parse(json);

    [Test]
    public async Task Compare_RepeatedNameOnBothSides_IsEquivalentToItself(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        const string Json = """{"a":1,"b":3,"a":2}""";

        var difference = JsonEquivalence.Compare(Json, Json, new JsonEquivalenceOptions());

        await Assert.That(difference).IsNull();
    }

    [Test]
    public async Task ContainsAll_RepeatedNameOnBothSides_ContainsItself(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        const string Json = """{"a":1,"b":3,"a":2}""";

        var differences = JsonEquivalence.ContainsAll(Json, Json, new JsonEquivalenceOptions());

        await Assert.That(differences).IsEmpty();
    }

    [Test]
    public async Task IsEquivalentJsonTo_RepeatedExpectedName_ComparesTheLastOccurrence(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That("""{"a":2}""").IsEquivalentJsonTo("""{"a":1,"a":2}""");
        await Assert.That("""{"a":1,"a":2}""").IsEquivalentJsonTo("""{"a":2}""");
    }

    [Test]
    public async Task ContainsAll_RepeatedExpectedNameMissingFromActual_ReportsItOnce(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var differences = JsonEquivalence.ContainsAll("""{"a":1,"a":1}""", "{}", new JsonEquivalenceOptions());

        await Assert.That(differences.Count).IsEqualTo(1);
        await Assert.That(differences[0].Kind).IsEqualTo(JsonDifferenceKind.MissingProperty);
    }

    [Test]
    public async Task Compare_RepeatedUnexpectedActualName_ReportsOneDifferenceForIt(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var expected = Parse("""{"a":1}""");
        using var actual = Parse("""{"x":1,"x":2,"a":1}""");
        var options = new JsonEquivalenceOptions();

        var difference = JsonEquivalence.Compare(expected.RootElement, actual.RootElement, options);

        await Assert.That(difference!.Kind).IsEqualTo(JsonDifferenceKind.UnexpectedProperty);
        await Assert.That(difference.Actual).IsEqualTo("2");
    }

    [Test]
    public async Task Canonicalize_RepeatedNames_KeepDocumentOrderBeyondTheInsertionSortLimit(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        const int Count = 60;
        var json = new StringBuilder("{");
        for (var i = 0; i < Count; i++)
        {
            json.Append(i is 0 ? string.Empty : ",").Append("\"k").Append((i % 3).ToString(CultureInfo.InvariantCulture))
                .Append("\":").Append(i.ToString(CultureInfo.InvariantCulture));
        }

        json.Append('}');
        var expected = new List<string>();
        for (var key = 0; key < 3; key++)
        {
            for (var i = key; i < Count; i += 3)
            {
                expected.Add("  \"k" + key.ToString(CultureInfo.InvariantCulture) + "\": " + i.ToString(CultureInfo.InvariantCulture));
            }
        }

        var result = JsonCanonicalizer.Canonicalize(json.ToString());

        await Assert.That(result).IsEqualTo("{\n" + string.Join(",\n", expected) + "\n}");
    }

    [Test]
    public async Task Compare_DefaultElement_Throws(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = Parse("{}");
        var options = new JsonEquivalenceOptions();
        var root = document.RootElement;

        var bothDefault = await Assert.That(() => JsonEquivalence.Compare(default(JsonElement), default(JsonElement), options)).Throws<ArgumentException>();
        var expectedDefault = await Assert.That(() => JsonEquivalence.Compare(default, root, options)).Throws<ArgumentException>();
        var actualDefault = await Assert.That(() => JsonEquivalence.Compare(root, default, options)).Throws<ArgumentException>();

        await Assert.That(bothDefault!.ParamName).IsEqualTo("expected");
        await Assert.That(expectedDefault!.ParamName).IsEqualTo("expected");
        await Assert.That(actualDefault!.ParamName).IsEqualTo("actual");
    }

    [Test]
    public async Task ContainsAll_DefaultElement_Throws(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = Parse("{}");
        var options = new JsonEquivalenceOptions();
        var root = document.RootElement;

        var bothDefault = await Assert.That(() => JsonEquivalence.ContainsAll(default(JsonElement), default(JsonElement), options)).Throws<ArgumentException>();
        var expectedDefault = await Assert.That(() => JsonEquivalence.ContainsAll(default, root, options)).Throws<ArgumentException>();
        var actualDefault = await Assert.That(() => JsonEquivalence.ContainsAll(root, default, options)).Throws<ArgumentException>();

        await Assert.That(bothDefault!.ParamName).IsEqualTo("expected");
        await Assert.That(expectedDefault!.ParamName).IsEqualTo("expected");
        await Assert.That(actualDefault!.ParamName).IsEqualTo("actual");
    }

    [Test]
    public async Task JsonPath_DefaultElement_Throws(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var resolve = await Assert.That(() => JsonPath.Resolve(default, "$")).Throws<ArgumentException>();
        await Assert.That(() => JsonPath.Exists(default, "$")).Throws<ArgumentException>();
        await Assert.That(() => JsonPath.ResolveAll(default, "items[*]")).Throws<ArgumentException>();

        await Assert.That(resolve!.ParamName).IsEqualTo("element");
    }
}
