using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using TUnit.Assertions.Exceptions;
using static JsonAssertions.TUnit.Tests.RealUseTestSupport;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// The full failure text of every assertion on every receiver, call line included, under the invariant culture,
/// <c>nl-NL</c> (a decimal comma) and <c>tr-TR</c> (the dotted and dotless i): the message rule of 0.7.0 pinned word for
/// word. The README samples are copied from these pins.
/// </summary>
[Timeout(30_000)]
internal sealed partial class FullMessagePinTests
{
    private const string Json = """{"user":{"name":"alice","age":31,"active":true,"nick":"","roles":["admin","dev"]},"items":[{"id":1},{"name":"b"}],"empty":[]}""";
    private const string Small = """{"a":1,"b":[1,2]}""";
    private static readonly string[] Cultures = ["", "nl-NL", "tr-TR"];

    [Test]
    public async Task HasJsonProperty_OnEveryReceiver(CancellationToken ct)
    {
        using var document = JsonDocument.Parse(Json);
        var element = document.RootElement;
        using var response = Buffered(Json);

        await PinAsync("HasJsonProperty, string", async () => await Assert.That(Json).HasJsonProperty("user.email"),
            "Expected to have a JSON property at path 'user.email'\n"
            + "but resolved as far as 'user', which has no property 'email'\n"
            + "\n"
            + "at Assert.That(Json).HasJsonProperty(\"user.email\")");
        await PinAsync("HasJsonProperty, element", async () => await Assert.That(element).HasJsonProperty("user.email"),
            "Expected to have a JSON property at path 'user.email'\n"
            + "but resolved as far as 'user', which has no property 'email'\n"
            + "\n"
            + "at Assert.That(element).HasJsonProperty(\"user.email\")");
        await PinAsync("HasJsonProperty, response", async () => await Assert.That(response).HasJsonProperty("user.email", ct),
            "Expected to have a JSON property at path 'user.email'\n"
            + "but resolved as far as 'user', which has no property 'email'\n"
            + "\n"
            + "at Assert.That(response).HasJsonProperty(\"user.email\", ct)");
    }

    [Test]
    public async Task HasJsonProperties_OnEveryReceiver(CancellationToken ct)
    {
        using var document = JsonDocument.Parse(Json);
        var element = document.RootElement;
        using var response = Buffered(Json);

        await PinAsync("HasJsonProperties, string", async () => await Assert.That(Json).HasJsonProperties(["user.name", "user.email", "items[*].id"]),
            "Expected to have a JSON property at each of the given paths\n"
            + "but 2 of 3 paths did not resolve:\n"
            + "  'user.email': resolved as far as 'user', which has no property 'email'\n"
            + "  'items[*].id': resolved as far as 'items[1]', which has no property 'id' (1 of 2 elements)\n"
            + "\n"
            + "at Assert.That(Json).HasJsonProperties([\"user.name\", \"user.email\", \"items[*].id\"])");
        await PinAsync("HasJsonProperties, element", async () => await Assert.That(element).HasJsonProperties(["user.name", "user.email", "items[*].id"]),
            "Expected to have a JSON property at each of the given paths\n"
            + "but 2 of 3 paths did not resolve:\n"
            + "  'user.email': resolved as far as 'user', which has no property 'email'\n"
            + "  'items[*].id': resolved as far as 'items[1]', which has no property 'id' (1 of 2 elements)\n"
            + "\n"
            + "at Assert.That(element).HasJsonProperties([\"user.name\", \"user.email\", \"items[*].id\"])");
        await PinAsync("HasJsonProperties, response", async () => await Assert.That(response).HasJsonProperties(["user.name", "user.email", "items[*].id"], ct),
            "Expected to have a JSON property at each of the given paths\n"
            + "but 2 of 3 paths did not resolve:\n"
            + "  'user.email': resolved as far as 'user', which has no property 'email'\n"
            + "  'items[*].id': resolved as far as 'items[1]', which has no property 'id' (1 of 2 elements)\n"
            + "\n"
            + "at Assert.That(response).HasJsonProperties([\"user.name\", \"user.email\", \"items[*].id\"], ct)");
    }

    [Test]
    public async Task DoesNotHaveJsonProperty_OnEveryReceiver(CancellationToken ct)
    {
        using var document = JsonDocument.Parse(Json);
        var element = document.RootElement;
        using var response = Buffered(Json);

        await PinAsync("DoesNotHaveJsonProperty, string", async () => await Assert.That(Json).DoesNotHaveJsonProperty("user.name"),
            "Expected to have no JSON property at path 'user.name'\n"
            + "but found \"alice\"\n"
            + "\n"
            + "at Assert.That(Json).DoesNotHaveJsonProperty(\"user.name\")");
        await PinAsync("DoesNotHaveJsonProperty, element", async () => await Assert.That(element).DoesNotHaveJsonProperty("user.roles"),
            "Expected to have no JSON property at path 'user.roles'\n"
            + "but found an Array\n"
            + "\n"
            + "at Assert.That(element).DoesNotHaveJsonProperty(\"user.roles\")");
        await PinAsync("DoesNotHaveJsonProperty, response", async () => await Assert.That(response).DoesNotHaveJsonProperty("user.name", ct),
            "Expected to have no JSON property at path 'user.name'\n"
            + "but found \"alice\"\n"
            + "\n"
            + "at Assert.That(response).DoesNotHaveJsonProperty(\"user.name\", ct)");
    }

    [Test]
    public async Task ScalarValues_OnAString(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await PinAsync("HasJsonValue string, string", async () => await Assert.That(Json).HasJsonValue("user.name", "bob"),
            "Expected to have the JSON string 'bob' at path 'user.name'\n"
            + "but found \"alice\"\n"
            + "\n"
            + "at Assert.That(Json).HasJsonValue(\"user.name\", \"bob\")");
        await PinAsync("HasJsonValue bool, string", async () => await Assert.That(Json).HasJsonValue("user.active", false),
            "Expected to have the given JSON boolean at path 'user.active'\n"
            + "but found true (expected false)\n"
            + "\n"
            + "at Assert.That(Json).HasJsonValue(\"user.active\", false)");
        await PinAsync("HasJsonValue double, string", async () => await Assert.That(Json).HasJsonValue("user.age", 30.5),
            "Expected to have the given JSON number at path 'user.age'\n"
            + "but found 31 (expected 30.5)\n"
            + "\n"
            + "at Assert.That(Json).HasJsonValue(\"user.age\", 30.5)");
        await PinAsync("HasJsonValue int, string", async () => await Assert.That(Json).HasJsonValue("user.age", 30),
            "Expected to have the JSON integer 30 at path 'user.age'\n"
            + "but found 31\n"
            + "\n"
            + "at Assert.That(Json).HasJsonValue(\"user.age\", 30)");
        await PinAsync("HasJsonValue uint, string", async () => await Assert.That(Json).HasJsonValue("user.name", 30U),
            "Expected to have the JSON integer 30 at path 'user.name'\n"
            + "but found \"alice\", which is not a JSON integer literal\n"
            + "\n"
            + "at Assert.That(Json).HasJsonValue(\"user.name\", 30U)");
        await PinAsync("HasJsonValue long, string", async () => await Assert.That(Json).HasJsonValue("user.age", 30L),
            "Expected to have the JSON integer 30 at path 'user.age'\n"
            + "but found 31\n"
            + "\n"
            + "at Assert.That(Json).HasJsonValue(\"user.age\", 30L)");
        await PinAsync("HasJsonValue ulong, string", async () => await Assert.That(Json).HasJsonValue("user.email", 30UL),
            "Expected to have the JSON integer 30 at path 'user.email'\n"
            + "but resolved as far as 'user', which has no property 'email'\n"
            + "\n"
            + "at Assert.That(Json).HasJsonValue(\"user.email\", 30UL)");
    }

    [Test]
    public async Task OneOfMatchingAndParsable_OnAString(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await PinAsync("HasJsonValueMatching, string", async () => await Assert.That(Json).HasJsonValueMatching("user.age", static e => e.GetInt32() > 40),
            "Expected to have a value matching the predicate at path 'user.age'\n"
            + "but found 31\n"
            + "\n"
            + "at Assert.That(Json).HasJsonValueMatching(\"user.age\", static e => e.GetInt32() > 40)");
        await PinAsync("HasJsonValueOneOf string, string", async () => await Assert.That(Json).HasJsonValueOneOf("user.name", ["bob", "carol"]),
            "Expected to have one of the given values at path 'user.name'\n"
            + "but found \"alice\" (candidates: \"bob\", \"carol\")\n"
            + "\n"
            + "at Assert.That(Json).HasJsonValueOneOf(\"user.name\", [\"bob\", \"carol\"])");
        await PinAsync("HasJsonValueOneOf double, string", async () => await Assert.That(Json).HasJsonValueOneOf("user.age", [30.5, 32.5]),
            "Expected to have one of the given values at path 'user.age'\n"
            + "but found 31 (candidates: 30.5, 32.5)\n"
            + "\n"
            + "at Assert.That(Json).HasJsonValueOneOf(\"user.age\", [30.5, 32.5])");
        await PinAsync("HasJsonValueOneOf int, string", async () => await Assert.That(Json).HasJsonValueOneOf("user.age", [30, 32]),
            "Expected to have one of the given values at path 'user.age'\n"
            + "but found 31 (candidates: 30, 32)\n"
            + "\n"
            + "at Assert.That(Json).HasJsonValueOneOf(\"user.age\", [30, 32])");
        await PinAsync("HasJsonValueOneOf uint, string", async () => await Assert.That(Json).HasJsonValueOneOf("user.age", [30U, 32U]),
            "Expected to have one of the given values at path 'user.age'\n"
            + "but found 31 (candidates: 30, 32)\n"
            + "\n"
            + "at Assert.That(Json).HasJsonValueOneOf(\"user.age\", [30U, 32U])");
        await PinAsync("HasJsonValueOneOf long, string", async () => await Assert.That(Json).HasJsonValueOneOf("user.age", [30L, 32L]),
            "Expected to have one of the given values at path 'user.age'\n"
            + "but found 31 (candidates: 30, 32)\n"
            + "\n"
            + "at Assert.That(Json).HasJsonValueOneOf(\"user.age\", [30L, 32L])");
        await PinAsync("HasJsonValueOneOf ulong, string", async () => await Assert.That(Json).HasJsonValueOneOf("user.age", [30UL, 32UL]),
            "Expected to have one of the given values at path 'user.age'\n"
            + "but found 31 (candidates: 30, 32)\n"
            + "\n"
            + "at Assert.That(Json).HasJsonValueOneOf(\"user.age\", [30UL, 32UL])");
        await PinAsync("HasJsonValueParsableAs, string", async () => await Assert.That(Json).HasJsonValueParsableAs<Guid>("user.name"),
            "Expected to have a JSON string parseable as the given type at path 'user.name'\n"
            + "but found \"alice\", which does not parse as Guid\n"
            + "\n"
            + "at Assert.That(Json).HasJsonValueParsableAs(\"user.name\")");
    }

    [Test]
    public async Task ScalarValues_OnAnElement(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse(Json);
        var element = document.RootElement;

        await PinAsync("HasJsonValue string, element", async () => await Assert.That(element).HasJsonValue("user.name", "bob"),
            "Expected to have the JSON string 'bob' at path 'user.name'\n"
            + "but found \"alice\"\n"
            + "\n"
            + "at Assert.That(element).HasJsonValue(\"user.name\", \"bob\")");
        await PinAsync("HasJsonValue bool, element", async () => await Assert.That(element).HasJsonValue("user.name", true),
            "Expected to have the given JSON boolean at path 'user.name'\n"
            + "but found \"alice\" (expected true)\n"
            + "\n"
            + "at Assert.That(element).HasJsonValue(\"user.name\", true)");
        await PinAsync("HasJsonValue double, element", async () => await Assert.That(element).HasJsonValue("user.age", 30.5),
            "Expected to have the given JSON number at path 'user.age'\n"
            + "but found 31 (expected 30.5)\n"
            + "\n"
            + "at Assert.That(element).HasJsonValue(\"user.age\", 30.5)");
        await PinAsync("HasJsonValue int, element", async () => await Assert.That(element).HasJsonValue("user.age", 30),
            "Expected to have the JSON integer 30 at path 'user.age'\n"
            + "but found 31\n"
            + "\n"
            + "at Assert.That(element).HasJsonValue(\"user.age\", 30)");
        await PinAsync("HasJsonValue uint, element", async () => await Assert.That(element).HasJsonValue("user.age", 30U),
            "Expected to have the JSON integer 30 at path 'user.age'\n"
            + "but found 31\n"
            + "\n"
            + "at Assert.That(element).HasJsonValue(\"user.age\", 30U)");
        await PinAsync("HasJsonValue long, element", async () => await Assert.That(element).HasJsonValue("user.age", 30L),
            "Expected to have the JSON integer 30 at path 'user.age'\n"
            + "but found 31\n"
            + "\n"
            + "at Assert.That(element).HasJsonValue(\"user.age\", 30L)");
        await PinAsync("HasJsonValue ulong, element", async () => await Assert.That(element).HasJsonValue("user.age", 30UL),
            "Expected to have the JSON integer 30 at path 'user.age'\n"
            + "but found 31\n"
            + "\n"
            + "at Assert.That(element).HasJsonValue(\"user.age\", 30UL)");
    }

    [Test]
    public async Task OneOfMatchingAndParsable_OnAnElement(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse(Json);
        var element = document.RootElement;

        await PinAsync("HasJsonValueMatching, element", async () => await Assert.That(element).HasJsonValueMatching("items[*].id", static e => e.GetInt32() > 0),
            "Expected to have a value matching the predicate at path 'items[*].id'\n"
            + "but 'items[1].id' did not resolve: resolved as far as 'items[1]', which has no property 'id'\n"
            + "\n"
            + "at Assert.That(element).HasJsonValueMatching(\"items[*].id\", static e => e.GetInt32() > 0)");
        await PinAsync("HasJsonValueOneOf string, element", async () => await Assert.That(element).HasJsonValueOneOf("user.name", ["bob", "carol"]),
            "Expected to have one of the given values at path 'user.name'\n"
            + "but found \"alice\" (candidates: \"bob\", \"carol\")\n"
            + "\n"
            + "at Assert.That(element).HasJsonValueOneOf(\"user.name\", [\"bob\", \"carol\"])");
        await PinAsync("HasJsonValueOneOf double, element", async () => await Assert.That(element).HasJsonValueOneOf("user.age", [30.5, 32.5]),
            "Expected to have one of the given values at path 'user.age'\n"
            + "but found 31 (candidates: 30.5, 32.5)\n"
            + "\n"
            + "at Assert.That(element).HasJsonValueOneOf(\"user.age\", [30.5, 32.5])");
        await PinAsync("HasJsonValueOneOf int, element", async () => await Assert.That(element).HasJsonValueOneOf("user.age", [30, 32]),
            "Expected to have one of the given values at path 'user.age'\n"
            + "but found 31 (candidates: 30, 32)\n"
            + "\n"
            + "at Assert.That(element).HasJsonValueOneOf(\"user.age\", [30, 32])");
        await PinAsync("HasJsonValueOneOf uint, element", async () => await Assert.That(element).HasJsonValueOneOf("user.age", [30U, 32U]),
            "Expected to have one of the given values at path 'user.age'\n"
            + "but found 31 (candidates: 30, 32)\n"
            + "\n"
            + "at Assert.That(element).HasJsonValueOneOf(\"user.age\", [30U, 32U])");
        await PinAsync("HasJsonValueOneOf long, element", async () => await Assert.That(element).HasJsonValueOneOf("user.age", [30L, 32L]),
            "Expected to have one of the given values at path 'user.age'\n"
            + "but found 31 (candidates: 30, 32)\n"
            + "\n"
            + "at Assert.That(element).HasJsonValueOneOf(\"user.age\", [30L, 32L])");
        await PinAsync("HasJsonValueOneOf ulong, element", async () => await Assert.That(element).HasJsonValueOneOf("user.age", [30UL, 32UL]),
            "Expected to have one of the given values at path 'user.age'\n"
            + "but found 31 (candidates: 30, 32)\n"
            + "\n"
            + "at Assert.That(element).HasJsonValueOneOf(\"user.age\", [30UL, 32UL])");
        await PinAsync("HasJsonValueParsableAs, element", async () => await Assert.That(element).HasJsonValueParsableAs<Guid>("user.age"),
            "Expected to have a JSON string parseable as the given type at path 'user.age'\n"
            + "but found 31\n"
            + "\n"
            + "at Assert.That(element).HasJsonValueParsableAs(\"user.age\")");
    }

    [Test]
    public async Task ScalarValues_OnAResponse(CancellationToken ct)
    {
        using var response = Buffered(Json);

        await PinAsync("HasJsonValue string, response", async () => await Assert.That(response).HasJsonValue("user.name", "bob", ct),
            "Expected to have the JSON string 'bob' at path 'user.name'\n"
            + "but found \"alice\"\n"
            + "\n"
            + "at Assert.That(response).HasJsonValue(\"user.name\", \"bob\", ct)");
        await PinAsync("HasJsonValue bool, response", async () => await Assert.That(response).HasJsonValue("user.active", false, ct),
            "Expected to have the given JSON boolean at path 'user.active'\n"
            + "but found true (expected false)\n"
            + "\n"
            + "at Assert.That(response).HasJsonValue(\"user.active\", false, ct)");
        await PinAsync("HasJsonValue double, response", async () => await Assert.That(response).HasJsonValue("user.age", 30.5, ct),
            "Expected to have the given JSON number at path 'user.age'\n"
            + "but found 31 (expected 30.5)\n"
            + "\n"
            + "at Assert.That(response).HasJsonValue(\"user.age\", 30.5, ct)");
        await PinAsync("HasJsonValue int, response", async () => await Assert.That(response).HasJsonValue("user.age", 30, ct),
            "Expected to have the JSON integer 30 at path 'user.age'\n"
            + "but found 31\n"
            + "\n"
            + "at Assert.That(response).HasJsonValue(\"user.age\", 30, ct)");
        await PinAsync("HasJsonValue uint, response", async () => await Assert.That(response).HasJsonValue("user.age", 30U, ct),
            "Expected to have the JSON integer 30 at path 'user.age'\n"
            + "but found 31\n"
            + "\n"
            + "at Assert.That(response).HasJsonValue(\"user.age\", 30U, ct)");
        await PinAsync("HasJsonValue long, response", async () => await Assert.That(response).HasJsonValue("user.age", 30L, ct),
            "Expected to have the JSON integer 30 at path 'user.age'\n"
            + "but found 31\n"
            + "\n"
            + "at Assert.That(response).HasJsonValue(\"user.age\", 30L, ct)");
        await PinAsync("HasJsonValue ulong, response", async () => await Assert.That(response).HasJsonValue("user.age", 30UL, ct),
            "Expected to have the JSON integer 30 at path 'user.age'\n"
            + "but found 31\n"
            + "\n"
            + "at Assert.That(response).HasJsonValue(\"user.age\", 30UL, ct)");
    }

    [Test]
    public async Task OneOfMatchingAndParsable_OnAResponse(CancellationToken ct)
    {
        using var response = Buffered(Json);

        await PinAsync("HasJsonValueMatching, response", async () => await Assert.That(response).HasJsonValueMatching("user.age", static e => e.GetInt32() > 40, ct),
            "Expected to have a value matching the predicate at path 'user.age'\n"
            + "but found 31\n"
            + "\n"
            + "at Assert.That(response).HasJsonValueMatching(\"user.age\", static e => e.GetInt32() > 40, ct)");
        await PinAsync("HasJsonValueOneOf string, response", async () => await Assert.That(response).HasJsonValueOneOf("user.name", ["bob", "carol"], ct),
            "Expected to have one of the given values at path 'user.name'\n"
            + "but found \"alice\" (candidates: \"bob\", \"carol\")\n"
            + "\n"
            + "at Assert.That(response).HasJsonValueOneOf(\"user.name\", [\"bob\", \"carol\"], ct)");
        await PinAsync("HasJsonValueOneOf double, response", async () => await Assert.That(response).HasJsonValueOneOf("user.age", [30.5, 32.5], ct),
            "Expected to have one of the given values at path 'user.age'\n"
            + "but found 31 (candidates: 30.5, 32.5)\n"
            + "\n"
            + "at Assert.That(response).HasJsonValueOneOf(\"user.age\", [30.5, 32.5], ct)");
        await PinAsync("HasJsonValueOneOf int, response", async () => await Assert.That(response).HasJsonValueOneOf("user.age", [30, 32], ct),
            "Expected to have one of the given values at path 'user.age'\n"
            + "but found 31 (candidates: 30, 32)\n"
            + "\n"
            + "at Assert.That(response).HasJsonValueOneOf(\"user.age\", [30, 32], ct)");
        await PinAsync("HasJsonValueOneOf uint, response", async () => await Assert.That(response).HasJsonValueOneOf("user.age", [30U, 32U], ct),
            "Expected to have one of the given values at path 'user.age'\n"
            + "but found 31 (candidates: 30, 32)\n"
            + "\n"
            + "at Assert.That(response).HasJsonValueOneOf(\"user.age\", [30U, 32U], ct)");
        await PinAsync("HasJsonValueOneOf long, response", async () => await Assert.That(response).HasJsonValueOneOf("user.age", [30L, 32L], ct),
            "Expected to have one of the given values at path 'user.age'\n"
            + "but found 31 (candidates: 30, 32)\n"
            + "\n"
            + "at Assert.That(response).HasJsonValueOneOf(\"user.age\", [30L, 32L], ct)");
        await PinAsync("HasJsonValueOneOf ulong, response", async () => await Assert.That(response).HasJsonValueOneOf("user.age", [30UL, 32UL], ct),
            "Expected to have one of the given values at path 'user.age'\n"
            + "but found 31 (candidates: 30, 32)\n"
            + "\n"
            + "at Assert.That(response).HasJsonValueOneOf(\"user.age\", [30UL, 32UL], ct)");
        await PinAsync("HasJsonValueParsableAs, response", async () => await Assert.That(response).HasJsonValueParsableAs<Guid>("user.name", ct),
            "Expected to have a JSON string parseable as the given type at path 'user.name'\n"
            + "but found \"alice\", which does not parse as Guid\n"
            + "\n"
            + "at Assert.That(response).HasJsonValueParsableAs(\"user.name\", ct)");
    }

    [Test]
    public async Task ArrayLength_OnEveryReceiver(CancellationToken ct)
    {
        using var document = JsonDocument.Parse(Json);
        var element = document.RootElement;
        using var response = Buffered(Json);

        await PinAsync("HasJsonArrayLength, string", async () => await Assert.That(Json).HasJsonArrayLength("user.roles", 3),
            "Expected to have a JSON array of length 3 at path 'user.roles'\n"
            + "but found an array of length 2\n"
            + "\n"
            + "at Assert.That(Json).HasJsonArrayLength(\"user.roles\", 3)");
        await PinAsync("HasJsonArrayLength, element", async () => await Assert.That(element).HasJsonArrayLength("user.name", 3),
            "Expected to have a JSON array of length 3 at path 'user.name'\n"
            + "but found a String\n"
            + "\n"
            + "at Assert.That(element).HasJsonArrayLength(\"user.name\", 3)");
        await PinAsync("HasJsonArrayLength, response", async () => await Assert.That(response).HasJsonArrayLength("user.roles", 3, ct),
            "Expected to have a JSON array of length 3 at path 'user.roles'\n"
            + "but found an array of length 2\n"
            + "\n"
            + "at Assert.That(response).HasJsonArrayLength(\"user.roles\", 3, ct)");
    }

    [Test]
    public async Task ArrayEmptiness_OnEveryReceiver(CancellationToken ct)
    {
        using var document = JsonDocument.Parse(Json);
        var element = document.RootElement;
        using var response = Buffered(Json);

        await PinAsync("HasNonEmptyJsonArray, string", async () => await Assert.That(Json).HasNonEmptyJsonArray("empty"),
            "Expected to have a non-empty JSON array at path 'empty'\n"
            + "but found an empty array\n"
            + "\n"
            + "at Assert.That(Json).HasNonEmptyJsonArray(\"empty\")");
        await PinAsync("HasNonEmptyJsonArray, element", async () => await Assert.That(element).HasNonEmptyJsonArray("empty"),
            "Expected to have a non-empty JSON array at path 'empty'\n"
            + "but found an empty array\n"
            + "\n"
            + "at Assert.That(element).HasNonEmptyJsonArray(\"empty\")");
        await PinAsync("HasNonEmptyJsonArray, response", async () => await Assert.That(response).HasNonEmptyJsonArray("empty", ct),
            "Expected to have a non-empty JSON array at path 'empty'\n"
            + "but found an empty array\n"
            + "\n"
            + "at Assert.That(response).HasNonEmptyJsonArray(\"empty\", ct)");
        await PinAsync("HasEmptyJsonArray, string", async () => await Assert.That(Json).HasEmptyJsonArray("user.roles"),
            "Expected to have an empty JSON array at path 'user.roles'\n"
            + "but found an array of length 2\n"
            + "\n"
            + "at Assert.That(Json).HasEmptyJsonArray(\"user.roles\")");
        await PinAsync("HasEmptyJsonArray, element", async () => await Assert.That(element).HasEmptyJsonArray("user.roles"),
            "Expected to have an empty JSON array at path 'user.roles'\n"
            + "but found an array of length 2\n"
            + "\n"
            + "at Assert.That(element).HasEmptyJsonArray(\"user.roles\")");
        await PinAsync("HasEmptyJsonArray, response", async () => await Assert.That(response).HasEmptyJsonArray("user.roles", ct),
            "Expected to have an empty JSON array at path 'user.roles'\n"
            + "but found an array of length 2\n"
            + "\n"
            + "at Assert.That(response).HasEmptyJsonArray(\"user.roles\", ct)");
    }

    [Test]
    public async Task ValueKind_OnEveryReceiver(CancellationToken ct)
    {
        using var document = JsonDocument.Parse(Json);
        var element = document.RootElement;
        using var response = Buffered(Json);

        await PinAsync("HasJsonValueKind, string", async () => await Assert.That(Json).HasJsonValueKind("user", JsonValueKind.Array),
            "Expected to have a JSON value of kind Array at path 'user'\n"
            + "but found an Object\n"
            + "\n"
            + "at Assert.That(Json).HasJsonValueKind(\"user\", JsonValueKind.Array)");
        await PinAsync("HasJsonValueKind, element", async () => await Assert.That(element).HasJsonValueKind("user.roles", JsonValueKind.Object),
            "Expected to have a JSON value of kind Object at path 'user.roles'\n"
            + "but found an array of length 2\n"
            + "\n"
            + "at Assert.That(element).HasJsonValueKind(\"user.roles\", JsonValueKind.Object)");
        await PinAsync("HasJsonValueKind, response", async () => await Assert.That(response).HasJsonValueKind("user", JsonValueKind.Array, ct),
            "Expected to have a JSON value of kind Array at path 'user'\n"
            + "but found an Object\n"
            + "\n"
            + "at Assert.That(response).HasJsonValueKind(\"user\", JsonValueKind.Array, ct)");
    }

    [Test]
    public async Task StringAndBooleanShapes_OnEveryReceiver(CancellationToken ct)
    {
        using var document = JsonDocument.Parse(Json);
        var element = document.RootElement;
        using var response = Buffered(Json);

        await PinAsync("HasNonEmptyJsonString, string", async () => await Assert.That(Json).HasNonEmptyJsonString("user.nick"),
            "Expected to have a non-empty JSON string at path 'user.nick'\n"
            + "but found \"\"\n"
            + "\n"
            + "at Assert.That(Json).HasNonEmptyJsonString(\"user.nick\")");
        await PinAsync("HasNonEmptyJsonString, element", async () => await Assert.That(element).HasNonEmptyJsonString("user.nick"),
            "Expected to have a non-empty JSON string at path 'user.nick'\n"
            + "but found \"\"\n"
            + "\n"
            + "at Assert.That(element).HasNonEmptyJsonString(\"user.nick\")");
        await PinAsync("HasNonEmptyJsonString, response", async () => await Assert.That(response).HasNonEmptyJsonString("user.nick", ct),
            "Expected to have a non-empty JSON string at path 'user.nick'\n"
            + "but found \"\"\n"
            + "\n"
            + "at Assert.That(response).HasNonEmptyJsonString(\"user.nick\", ct)");
        await PinAsync("HasJsonBoolean, string", async () => await Assert.That(Json).HasJsonBoolean("user.name"),
            "Expected to have a JSON boolean at path 'user.name'\n"
            + "but found \"alice\"\n"
            + "\n"
            + "at Assert.That(Json).HasJsonBoolean(\"user.name\")");
        await PinAsync("HasJsonBoolean, element", async () => await Assert.That(element).HasJsonBoolean("user.name"),
            "Expected to have a JSON boolean at path 'user.name'\n"
            + "but found \"alice\"\n"
            + "\n"
            + "at Assert.That(element).HasJsonBoolean(\"user.name\")");
        await PinAsync("HasJsonBoolean, response", async () => await Assert.That(response).HasJsonBoolean("user.name", ct),
            "Expected to have a JSON boolean at path 'user.name'\n"
            + "but found \"alice\"\n"
            + "\n"
            + "at Assert.That(response).HasJsonBoolean(\"user.name\", ct)");
    }

    [Test]
    public async Task Equivalence_OnEveryReceiver(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var document = JsonDocument.Parse(Small);
        var element = document.RootElement;

        await PinAsync("IsEquivalentJsonTo, string", async () => await Assert.That(Small).IsEquivalentJsonTo("""{"a":2,"b":[1,2]}"""),
            "Expected to be JSON-equivalent to the expected document\n"
            + "but at 'a' the value differs\n"
            + "  expected: 2\n"
            + "  actual:   1\n"
            + "\n"
            + "at Assert.That(Small).IsEquivalentJsonTo(\"\"\"{\"a\":2,\"b\":[1,2]}\"\"\")");
        await PinAsync("IsEquivalentJsonTo configured, string", async () => await Assert.That(Small).IsEquivalentJsonTo("""{"a":1,"b":[2,1,3]}""", static o => o.IgnoreArrayOrder()),
            "Expected to be JSON-equivalent to the expected document\n"
            + "but at 'b' the array length differs\n"
            + "  expected: 3 element(s)\n"
            + "  actual:   2 element(s)\n"
            + "\n"
            + "at Assert.That(Small).IsEquivalentJsonTo(\"\"\"{\"a\":1,\"b\":[2,1,3]}\"\"\", static o => o.IgnoreArrayOrder())");
        await PinAsync("IsEquivalentJsonTo, element", async () => await Assert.That(element).IsEquivalentJsonTo("""{"a":1}"""),
            "Expected to be JSON-equivalent to the expected document\n"
            + "but at 'b' the property is not in the expected document\n"
            + "  expected: absent\n"
            + "  actual:   [1,2]\n"
            + "\n"
            + "at Assert.That(element).IsEquivalentJsonTo(\"\"\"{\"a\":1}\"\"\")");
        await PinAsync("IsEquivalentJsonTo configured, element", async () => await Assert.That(element).IsEquivalentJsonTo("""{"a":"1"}""", static o => o.IgnorePath("b")),
            "Expected to be JSON-equivalent to the expected document\n"
            + "but at 'a' the JSON kind differs\n"
            + "  expected: \"1\"\n"
            + "  actual:   1\n"
            + "\n"
            + "at Assert.That(element).IsEquivalentJsonTo(\"\"\"{\"a\":\"1\"}\"\"\", static o => o.IgnorePath(\"b\"))");
    }

    [Test]
    public async Task Contains_OnEveryReceiver(CancellationToken ct)
    {
        using var document = JsonDocument.Parse(Small);
        var element = document.RootElement;
        using var response = Buffered(Small);

        await PinAsync("ContainsJson, string", async () => await Assert.That(Small).ContainsJson("""{"a":2,"c":3}"""),
            "Expected to contain the expected JSON\n"
            + "but 2 paths differ:\n"
            + "  at 'a' the value differs\n"
            + "    expected: 2\n"
            + "    actual:   1\n"
            + "  at 'c' the property is missing from the actual document\n"
            + "    expected: 3\n"
            + "    actual:   absent\n"
            + "\n"
            + "at Assert.That(Small).ContainsJson(\"\"\"{\"a\":2,\"c\":3}\"\"\")");
        await PinAsync("ContainsJson configured, string", async () => await Assert.That(Small).ContainsJson("""{"b":[3]}""", static o => o.IgnoreArrayOrder()),
            "Expected to contain the expected JSON\n"
            + "but 1 path differs:\n"
            + "  at 'b[0]' no element of the actual array is equivalent\n"
            + "    expected: 3\n"
            + "\n"
            + "at Assert.That(Small).ContainsJson(\"\"\"{\"b\":[3]}\"\"\", static o => o.IgnoreArrayOrder())");
        await PinAsync("ContainsJson, element", async () => await Assert.That(element).ContainsJson("""{"c":3}"""),
            "Expected to contain the expected JSON\n"
            + "but 1 path differs:\n"
            + "  at 'c' the property is missing from the actual document\n"
            + "    expected: 3\n"
            + "    actual:   absent\n"
            + "\n"
            + "at Assert.That(element).ContainsJson(\"\"\"{\"c\":3}\"\"\")");
        await PinAsync("ContainsJson configured, element", async () => await Assert.That(element).ContainsJson("""{"a":2}""", static o => o.IgnorePath("b")),
            "Expected to contain the expected JSON\n"
            + "but 1 path differs:\n"
            + "  at 'a' the value differs\n"
            + "    expected: 2\n"
            + "    actual:   1\n"
            + "\n"
            + "at Assert.That(element).ContainsJson(\"\"\"{\"a\":2}\"\"\", static o => o.IgnorePath(\"b\"))");
        await PinAsync("ContainsJson, response", async () => await Assert.That(response).ContainsJson("""{"a":2}""", ct),
            "Expected to contain the expected JSON\n"
            + "but 1 path differs:\n"
            + "  at 'a' the value differs\n"
            + "    expected: 2\n"
            + "    actual:   1\n"
            + "\n"
            + "at Assert.That(response).ContainsJson(\"\"\"{\"a\":2}\"\"\", ct)");
        await PinAsync("ContainsJson configured, response", async () => await Assert.That(response).ContainsJson("""{"b":[3]}""", static o => o.IgnoreArrayOrder(), ct),
            "Expected to contain the expected JSON\n"
            + "but 1 path differs:\n"
            + "  at 'b[0]' no element of the actual array is equivalent\n"
            + "    expected: 3\n"
            + "\n"
            + "at Assert.That(response).ContainsJson(\"\"\"{\"b\":[3]}\"\"\", static o => o.IgnoreArrayOrder(), ct)");
    }

    [Test]
    public async Task ResponseOnlyAssertions(CancellationToken ct)
    {
        using var dto = Buffered("""{"Id":2,"Name":"b"}""");
        using var problem = Buffered("""{"status":400,"title":"Bad"}""", HttpStatusCode.BadRequest, "application/problem+json");
        using var validation = Buffered("""{"status":400,"errors":{"Age":["too low"],"Name":["required"]}}""", HttpStatusCode.BadRequest, "application/problem+json");
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal) { ["Age"] = ["too high"], ["Name"] = ["required"], ["Email"] = ["invalid"] };

        await PinAsync(
            "HasJsonResponse",
            async () => await Assert.That(dto).HasJsonResponse(HttpStatusCode.OK, HasJsonResponseTests.TestJsonContext.Default.TestDto, new HasJsonResponseTests.TestDto(1, "a"), ct),
            "Expected to have status OK and a JSON body equal to TestDto { Id = 1, Name = a }\n"
            + "but the body deserialized to TestDto { Id = 2, Name = b }\n"
            + "\n"
            + "at Assert.That(dto).HasJsonResponse(HttpStatusCode.OK, HasJsonResponseTests.TestJsonContext.Default.TestDto, new HasJsonResponseTests.TestDto(1, \"a\"), ct)");
        await PinAsync("MatchesProblemDetails", async () => await Assert.That(problem).MatchesProblemDetails(404, title: "Not found", cancellationToken: ct),
            "Expected to be an RFC 7807 problem response with status 404\n"
            + "but 3 fields differed:\n"
            + "  HTTP status: 400 BadRequest (expected 404)\n"
            + "  status: 400 (expected 404)\n"
            + "  title: \"Bad\" (expected \"Not found\")\n"
            + "\n"
            + "at Assert.That(problem).MatchesProblemDetails(404, \"Not found\", , , , ct)");
        await PinAsync("MatchesValidationProblemDetails", async () => await Assert.That(validation).MatchesValidationProblemDetails(422, errors, cancellationToken: ct),
            "Expected to be an RFC 7807 validation problem response with status 422 and the expected errors\n"
            + "but 4 entries differed:\n"
            + "  HTTP status: 400 BadRequest (expected 422)\n"
            + "  status: 400 (expected 422)\n"
            + "  errors.Age: [\"too low\"] (expected [\"too high\"])\n"
            + "  errors.Email: absent (expected [\"invalid\"])\n"
            + "\n"
            + "at Assert.That(validation).MatchesValidationProblemDetails(422, errors, , , , , ct)");
    }

    [Test]
    public async Task SerializerAssertions(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await PinAsync(
            "RoundtripsCleanlyVia",
            async () => await Assert.That(new PinDrifter(7)).RoundtripsCleanlyVia(PinDrifterContext.Default.PinDrifter),
            "Expected to serialize the same after a round trip through its JsonTypeInfo\n"
            + "but the second serialization differed:\n"
            + "  first:  {\"v\":7}\n"
            + "  second: {\"v\":7,\"drift\":true}\n"
            + "\n"
            + "at Assert.That(new PinDrifter(7)).RoundtripsCleanlyVia(PinDrifterContext.Default.PinDrifter)");
        await PinAsync(
            "HasJsonTypeInfoFor",
            async () => await Assert.That(HasJsonTypeInfoForTests.ContextRegistrationJsonContext.Default).AsJsonContext().HasJsonTypeInfoFor<DateTime>(),
            "Expected to register JsonTypeInfo<DateTime>\n"
            + "but ContextRegistrationJsonContext.GetTypeInfo(typeof(DateTime)) returned null; add [JsonSerializable(typeof(DateTime))] to ContextRegistrationJsonContext\n"
            + "\n"
            + "at Assert.That(HasJsonTypeInfoForTests.ContextRegistrationJsonContext.Default).AsJsonContext().HasJsonTypeInfoFor<DateTime>()");
    }

    /// <summary>The texts that are the same for every receiver, pinned once on the receiver that shows them best.</summary>
    [Test]
    public async Task SharedTexts_BodiesAndDocuments(CancellationToken ct)
    {
        using var malformed = Buffered("""{"user":""");
        using var empty = Buffered(string.Empty);

        await PinAsync("malformed body", async () => await Assert.That(malformed).HasJsonProperty("user", ct),
            "Expected to have a JSON property at path 'user'\n"
            + "but the value is not valid JSON: Expected depth to be zero at the end of the JSON payload. There is an open JSON object or array that should be closed. LineNumber: 0 | BytePositionInLine: 8.\n"
            + "\n"
            + "at Assert.That(malformed).HasJsonProperty(\"user\", ct)");
        await PinAsync("malformed expected", async () => await Assert.That(Small).IsEquivalentJsonTo("{a"),
            "Expected to be JSON-equivalent to the expected document\n"
            + "but the expected document is not valid JSON: 'a' is an invalid start of a property name. Expected a '\"'. LineNumber: 0 | BytePositionInLine: 1.\n"
            + "\n"
            + "at Assert.That(Small).IsEquivalentJsonTo(\"{a\")");
        await PinAsync("empty body", async () => await Assert.That(empty).HasJsonProperty("user", ct),
            "Expected to have a JSON property at path 'user'\n"
            + "but the response body is empty; a body read through its stream before the assertion is gone, unless the content was buffered first with LoadIntoBufferAsync\n"
            + "\n"
            + "at Assert.That(empty).HasJsonProperty(\"user\", ct)");
        await PinAsync("default element", async () => await Assert.That(default(JsonElement)).HasJsonProperty("user"),
            "Expected to have a JSON property at path 'user'\n"
            + "but the JsonElement is default: it holds no JSON value\n"
            + "\n"
            + "at Assert.That(default(JsonElement)).HasJsonProperty(\"user\")");
    }

    [Test]
    public async Task SharedTexts_ResponsesAndPaths(CancellationToken ct)
    {
        using var badStatus = Buffered("""{"error":"x"}""", HttpStatusCode.BadRequest);
        using var badBody = Buffered("{bad");
        using var notProblem = Buffered("""{"status":400}""", HttpStatusCode.BadRequest);

        await PinAsync(
            "HasJsonResponse status",
            async () => await Assert.That(badStatus).HasJsonResponse(HttpStatusCode.OK, HasJsonResponseTests.TestJsonContext.Default.TestDto, new HasJsonResponseTests.TestDto(1, "a"), ct),
            "Expected to have status OK and a JSON body equal to TestDto { Id = 1, Name = a }\n"
            + "but the status was 400 BadRequest; body: {\"error\":\"x\"}\n"
            + "\n"
            + "at Assert.That(badStatus).HasJsonResponse(HttpStatusCode.OK, HasJsonResponseTests.TestJsonContext.Default.TestDto, new HasJsonResponseTests.TestDto(1, \"a\"), ct)");
        await PinAsync(
            "HasJsonResponse body",
            async () => await Assert.That(badBody).HasJsonResponse(HttpStatusCode.OK, HasJsonResponseTests.TestJsonContext.Default.TestDto, new HasJsonResponseTests.TestDto(1, "a"), ct),
            "Expected to have status OK and a JSON body equal to TestDto { Id = 1, Name = a }\n"
            + "but the body did not deserialize as TestDto: 'b' is an invalid start of a property name. Expected a '\"'. Path: $ | LineNumber: 0 | BytePositionInLine: 1.; body: {bad\n"
            + "\n"
            + "at Assert.That(badBody).HasJsonResponse(HttpStatusCode.OK, HasJsonResponseTests.TestJsonContext.Default.TestDto, new HasJsonResponseTests.TestDto(1, \"a\"), ct)");
        await PinAsync("ProblemDetails media type", async () => await Assert.That(notProblem).MatchesProblemDetails(400, cancellationToken: ct),
            "Expected to be an RFC 7807 problem response with status 400\n"
            + "but the Content-Type was application/json, not application/problem+json; body: {\"status\":400}\n"
            + "\n"
            + "at Assert.That(notProblem).MatchesProblemDetails(400, , , , , ct)");
        await PinAsync("wildcard index", async () => await Assert.That(Json).HasJsonProperty("items[*].id"),
            "Expected to have a JSON property at path 'items[*].id'\n"
            + "but resolved as far as 'items[1]', which has no property 'id'\n"
            + "\n"
            + "at Assert.That(Json).HasJsonProperty(\"items[*].id\")");
        await PinAsync("index out of range", async () => await Assert.That(Json).HasJsonProperty("user.roles[5]"),
            "Expected to have a JSON property at path 'user.roles[5]'\n"
            + "but resolved as far as 'user.roles', which has 2 elements, so no index [5]\n"
            + "\n"
            + "at Assert.That(Json).HasJsonProperty(\"user.roles[5]\")");
        await PinAsync("integer literal", async () => await Assert.That("""{"v":"+42"}""").HasJsonValue("v", 42),
            "Expected to have the JSON integer 42 at path 'v'\n"
            + "but found \"+42\", which is not a JSON integer literal\n"
            + "\n"
            + "at Assert.That(\"\"\"{\"v\":\"+42\"}\"\"\").HasJsonValue(\"v\", 42)");
    }

    [Test]
    public async Task ExtractionMessages(CancellationToken ct)
    {
        using var document = JsonDocument.Parse(Json);
        var element = document.RootElement;
        using var response = Buffered(Json);

        await PinAsync("GetJsonValue, element", () => Task.FromResult(element.GetJsonValue<int>("user.name")),
            "The value at 'user.name' is \"alice\", which does not parse as Int32");
        await PinAsync("GetJsonValue, string", () => Task.FromResult(Json.GetJsonValue<int>("user.email")),
            "No JSON property at path 'user.email': resolved as far as 'user', which has no property 'email'");
        await PinAsync("GetJsonValue, response", async () => await response.GetJsonValueAsync<int>("user.name", ct),
            "The value at 'user.name' is \"alice\", which does not parse as Int32");
        await PinAsync("GetJsonString, element", () => Task.FromResult(element.GetJsonString("user.age")),
            "The value at 'user.age' is 31, which is not a JSON string");
        await PinAsync("GetJsonString, string", () => Task.FromResult(Json.GetJsonString("user.roles")),
            "The value at 'user.roles' is an Array, which is not a JSON string");
        await PinAsync("GetJsonString, response", async () => await response.GetJsonStringAsync("user.age", ct),
            "The value at 'user.age' is 31, which is not a JSON string");
        await PinAsync("GetJsonElement, element", () => Task.FromResult(element.GetJsonElement("user.email")),
            "No JSON property at path 'user.email': resolved as far as 'user', which has no property 'email'");
        await PinAsync("GetJsonElement, string", () => Task.FromResult(Json.GetJsonElement("user.name.first")),
            "No JSON property at path 'user.name.first': resolved as far as 'user.name', which is a String, not an object");
        await PinAsync("GetJsonElement, response", async () => await response.GetJsonElementAsync("user.roles[5]", ct),
            "No JSON property at path 'user.roles[5]': resolved as far as 'user.roles', which has 2 elements, so no index [5]");
        await PinAsync("GetJsonValues, element", () => Task.FromResult(element.GetJsonValues<int>("items[*].id")),
            "Could not read 1 of 2 values at 'items[*].id':\n"
            + "  'items[1].id': resolved as far as 'items[1]', which has no property 'id'");
        await PinAsync("GetJsonValues, string", () => Task.FromResult(Json.GetJsonValues<Guid>("user.roles[*]")),
            "Could not read 2 of 2 values at 'user.roles[*]':\n"
            + "  'user.roles[0]': found \"admin\", which does not parse as Guid\n"
            + "  'user.roles[1]': found \"dev\", which does not parse as Guid");
        await PinAsync("GetJsonValues, response", async () => await response.GetJsonValuesAsync<int>("user[*]", ct),
            "Could not read 1 of 1 value at 'user[*]':\n"
            + "  'user[*]': cannot expand [*]: 'user' is an Object, not an array");
        await PinAsync("malformed text", () => Task.FromResult("{".GetJsonString("a")),
            "The value is not valid JSON: Expected depth to be zero at the end of the JSON payload. There is an open JSON object or array that should be closed. LineNumber: 0 | BytePositionInLine: 1.");
    }

    /// <summary>The README's consumer-authored assertion (<see cref="OrderAssertions"/>) reads like the package's
    /// own.</summary>
    [Test]
    public async Task ConsumerExtensionSample(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var body = """{"order":{"status":"Pending"}}""";
        var noStatus = """{"order":{}}""";

        await PinAsync("consumer sample, value", async () => await Assert.That(body).HasOrderStatusOf("Paid"),
            "Expected to have order status 'Paid'\n"
            + "but found \"Pending\"\n"
            + "\n"
            + "at Assert.That(body).HasOrderStatusOf(\"Paid\")");
        await PinAsync("consumer sample, path", async () => await Assert.That(noStatus).HasOrderStatusOf("Paid"),
            "Expected to have order status 'Paid'\n"
            + "but resolved as far as 'order', which has no property 'status'\n"
            + "\n"
            + "at Assert.That(noStatus).HasOrderStatusOf(\"Paid\")");
    }

    /// <summary>The one text that follows the current culture: <c>HasJsonResponse</c> prints the expected and the
    /// deserialized value with their own <c>ToString</c>, so a <see cref="decimal"/> reads <c>2,5</c> under
    /// <c>nl-NL</c> and <c>tr-TR</c>. Everything around the two values reads the same in each culture.</summary>
    [Test]
    public async Task HasJsonResponse_ValuesFollowTheCurrentCulture(CancellationToken ct)
    {
        using var priced = Buffered("""{"Price":2.5}""");
        string[] separators = [".", ",", ","];

        for (var i = 0; i < Cultures.Length; i++)
        {
            var message = await MessageInCultureAsync(
                Cultures[i],
                async () => await Assert.That(priced).HasJsonResponse(HttpStatusCode.OK, PinPricedContext.Default.PricedDto, new PricedDto(1.5m), ct));

            await Assert.That(message).IsEqualTo(
                "Expected to have status OK and a JSON body equal to PricedDto { Price = 1" + separators[i] + "5 }\n"
                + "but the body deserialized to PricedDto { Price = 2" + separators[i] + "5 }\n"
                + "\n"
                + "at Assert.That(priced).HasJsonResponse(HttpStatusCode.OK, PinPricedContext.Default.PricedDto, new PricedDto(1.5m), ct)")
                .Because("under '" + Cultures[i] + "'");
        }
    }

    /// <summary>Runs <paramref name="failing"/> under each culture and checks that each failure text equals
    /// <paramref name="expected"/>.</summary>
    private static async Task PinAsync(string label, Func<Task> failing, string expected)
    {
        foreach (var culture in Cultures)
        {
            var message = await MessageInCultureAsync(culture, failing);
            await Assert.That(message).IsEqualTo(expected).Because(label + " under '" + culture + "'");
        }
    }

    /// <summary>The failure text of <paramref name="failing"/> under <paramref name="culture"/> (empty for the
    /// invariant culture).</summary>
    private static async Task<string> MessageInCultureAsync(string culture, Func<Task> failing)
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = culture.Length is 0 ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo(culture);
            return await MessageOfAsync(failing);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    /// <summary>A value whose text depends on the culture, for <see cref="HasJsonResponse_ValuesFollowTheCurrentCulture"/>.</summary>
    internal sealed record PricedDto(decimal Price);

    [JsonSerializable(typeof(PricedDto))]
    internal sealed partial class PinPricedContext : JsonSerializerContext;

    /// <summary>A value whose second serialization differs from its first, every time: a value read back from JSON
    /// writes an extra property. Stateless, unlike <see cref="RoundtripsCleanlyViaTests.Drifter"/>, so the pin holds in
    /// each culture.</summary>
    [JsonConverter(typeof(PinDrifterConverter))]
    internal sealed record PinDrifter(int V)
    {
        /// <summary>Whether the value was read back from JSON.</summary>
        internal bool WasRead { get; init; }
    }

    internal sealed class PinDrifterConverter : JsonConverter<PinDrifter>
    {
        public override PinDrifter Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            return new PinDrifter(doc.RootElement.GetProperty("v").GetInt32()) { WasRead = true };
        }

        public override void Write(Utf8JsonWriter writer, PinDrifter value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("v", value.V);
            if (value.WasRead)
            {
                writer.WriteBoolean("drift", true);
            }

            writer.WriteEndObject();
        }
    }

    [JsonSerializable(typeof(PinDrifter))]
    internal sealed partial class PinDrifterContext : JsonSerializerContext;

    private static async Task<string> MessageOfAsync(Func<Task> failing)
    {
        try
        {
            await failing();
            return "(passed)";
        }
        catch (AssertionException ex)
        {
            return ex.Message;
        }
        catch (JsonExtractionException ex)
        {
            return ex.Message;
        }
    }
}
