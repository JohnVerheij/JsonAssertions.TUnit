using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TUnit.Assertions.Exceptions;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// End-to-end tests for the <c>[GenerateAssertion]</c>-emitted shape chains:
/// <c>HasJsonArrayLength</c>, <c>HasNonEmptyJsonArray</c>, <c>HasEmptyJsonArray</c>, and
/// <c>HasJsonValueKind</c> over a JSON <see cref="string"/> and a <see cref="JsonElement"/>.
/// The failing cases inspect the message so both <c>JsonFailureMessage.ShapeMismatch</c>
/// branches (path resolved but wrong shape; path did not resolve) and the found-shape
/// rendering (an array reports its length; any other kind reports its kind) are exercised.
/// </summary>
[Category("Smoke")]
[Timeout(5_000)]
internal sealed class JsonShapeAssertionsTests
{
    private const string SampleJson = """{"items":[1,2,3],"empty":[],"name":"alice","meta":{}}""";

    [Test]
    public async Task HasJsonArrayLength_Match_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(SampleJson).HasJsonArrayLength("items", 3);
    }

    [Test]
    public async Task HasJsonArrayLength_Match_JsonElement_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse(SampleJson);

        await Assert.That(document.RootElement).HasJsonArrayLength("items", 3);
    }

    [Test]
    public async Task HasJsonArrayLength_WrongLength_FailsShowingFoundLength(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That(SampleJson).HasJsonArrayLength("items", 5);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("to have a JSON array of length 5 at path \"items\"");
        await Assert.That(ex.Message).Contains("found: an array of length 3");
    }

    [Test]
    public async Task HasJsonArrayLength_NotAnArray_FailsShowingFoundKind(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That(SampleJson).HasJsonArrayLength("name", 1);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("found: a String");
    }

    [Test]
    public async Task HasJsonArrayLength_PathNotFound_FailsWithFailurePoint(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That(SampleJson).HasJsonArrayLength("missing", 1);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("resolved as far as: (root)");
    }

    [Test]
    public async Task HasNonEmptyJsonArray_NonEmpty_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(SampleJson).HasNonEmptyJsonArray("items");
    }

    [Test]
    public async Task HasNonEmptyJsonArray_Empty_Fails(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That(SampleJson).HasNonEmptyJsonArray("empty");
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("to have a non-empty JSON array at path \"empty\"");
        await Assert.That(ex.Message).Contains("found: an array of length 0");
    }

    [Test]
    public async Task HasEmptyJsonArray_Empty_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(SampleJson).HasEmptyJsonArray("empty");
    }

    [Test]
    public async Task HasEmptyJsonArray_NonEmpty_Fails(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That(SampleJson).HasEmptyJsonArray("items");
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("to have an empty JSON array at path \"items\"");
    }

    [Test]
    public async Task HasJsonValueKind_Match_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Assert.That(SampleJson).HasJsonValueKind("meta", JsonValueKind.Object);
    }

    [Test]
    public async Task HasJsonValueKind_Match_JsonElement_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse(SampleJson);

        await Assert.That(document.RootElement).HasJsonValueKind("items", JsonValueKind.Array);
    }

    [Test]
    public async Task HasJsonValueKind_Mismatch_FailsShowingExpectedAndFound(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var ex = await Assert.That(async () =>
        {
            await Assert.That(SampleJson).HasJsonValueKind("name", JsonValueKind.Array);
        }).Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("to have a JSON value of kind Array at path \"name\"");
        await Assert.That(ex.Message).Contains("found: a String");
    }
}
