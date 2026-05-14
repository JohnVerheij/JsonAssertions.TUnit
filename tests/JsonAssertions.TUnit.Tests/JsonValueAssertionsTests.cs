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
}
