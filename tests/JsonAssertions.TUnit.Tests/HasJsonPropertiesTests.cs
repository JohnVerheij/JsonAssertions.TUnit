using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TUnit.Assertions.Exceptions;
using static JsonAssertions.TUnit.Tests.RealUseTestSupport;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// <c>HasJsonProperties</c> asserts several paths against one parse: every path that does not resolve is listed, in
/// the given order, and a bad path list throws before the JSON is read.
/// </summary>
[Timeout(10_000)]
internal sealed class HasJsonPropertiesTests
{
    private const string User = """{"user":{"name":"alice","age":31},"items":[{"id":1},{"name":"b"}],"roles":["admin"],"empty":[]}""";

    [Test]
    public async Task AllPresent_PassesOnEveryReceiver(CancellationToken ct)
    {
        using var document = JsonDocument.Parse(User);
        using var response = Buffered(User);
        using var responseWithoutToken = Buffered(User);

        await Assert.That(User).HasJsonProperties(["user.name", "user.age", "items[*]", "roles[0]"]);
        await Assert.That(document.RootElement).HasJsonProperties(["user.name", "roles[0]"]);
        await Assert.That(response).HasJsonProperties(["user.name", "user.age"], ct);
        await Assert.That(responseWithoutToken).HasJsonProperties(["user"], CancellationToken.None).And.HasJsonProperty("roles[0]", ct);
    }

    /// <summary>The scenario of the design draft, pinned in full.</summary>
    [Test]
    public async Task SomeMissing_ListsEachPathThatDidNotResolve(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var ex = await Assert.That(async () => await Assert.That(User).HasJsonProperties(["user.name", "user.email", "items[*].id", "roles[0]"]))
            .Throws<AssertionException>();

        await Assert.That(ex!.Message).IsEqualTo(
            "Expected to have a JSON property at each of the given paths\n"
            + "but 2 of 4 paths did not resolve:\n"
            + "  'user.email': resolved as far as 'user', which has no property 'email'\n"
            + "  'items[*].id': resolved as far as 'items[1]', which has no property 'id' (1 of 2 elements)\n"
            + "\n"
            + "at Assert.That(User).HasJsonProperties([\"user.name\", \"user.email\", \"items[*].id\", \"roles[0]\"])");
    }

    [Test]
    public async Task AllMissing_ListsEveryPathWithItsReason(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var ex = await Assert.That(async () => await Assert.That(User).HasJsonProperties(["account", "user.name.first", "roles[5]", "user[0]", "user[*]"]))
            .Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains(
            "but 5 of 5 paths did not resolve:\n"
            + "  'account': resolved as far as the root, which has no property 'account'\n"
            + "  'user.name.first': resolved as far as 'user.name', which is a String, not an object\n"
            + "  'roles[5]': resolved as far as 'roles', which has 1 element, so no index [5]\n"
            + "  'user[0]': resolved as far as 'user', which is an Object, not an array\n"
            + "  'user[*]': cannot expand [*]: 'user' is an Object, not an array\n");
    }

    [Test]
    public async Task OnePathOfOne_CountsInTheSingular(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var ex = await Assert.That(async () => await Assert.That(User).HasJsonProperties(["items[2]"]))
            .Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("but 1 of 1 path did not resolve:\n  'items[2]': resolved as far as 'items', which has 2 elements, so no index [2]");
    }

    /// <summary>A <c>[*]</c> over an empty array is a "for all" over nothing, as in <c>HasJsonProperty</c>.</summary>
    [Test]
    public async Task WildcardOverAnEmptyArray_Passes(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await Assert.That(User).HasJsonProperties(["empty[*].id", "user.name"]);
    }

    [Test]
    public async Task EmptyNullOrRepeatedPaths_Throw(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await Assert.That(async () => await Assert.That(User).HasJsonProperties([]))
            .Throws<ArgumentException>().WithMessageContaining("No paths were given", StringComparison.Ordinal);
        await Assert.That(async () => await Assert.That(User).HasJsonProperties(null!))
            .Throws<ArgumentNullException>();
        await Assert.That(async () => await Assert.That(User).HasJsonProperties(["user", null!]))
            .Throws<ArgumentException>().WithMessageContaining("The path at paths[1] is null, empty or white space.", StringComparison.Ordinal);
        await Assert.That(async () => await Assert.That(User).HasJsonProperties(["user", " "]))
            .Throws<ArgumentException>().WithMessageContaining("paths[1]", StringComparison.Ordinal);
        await Assert.That(async () => await Assert.That(User).HasJsonProperties(["user", "roles", "user"]))
            .Throws<ArgumentException>().WithMessageContaining("Path 'user' (paths[2]) repeats an earlier path", StringComparison.Ordinal);
        await Assert.That(async () => await Assert.That(User).HasJsonProperties(["user.name", "$.user.name"]))
            .Throws<ArgumentException>().WithMessageContaining("Path '$.user.name' (paths[1]) repeats an earlier path", StringComparison.Ordinal);
        await Assert.That(User).HasJsonProperties(["$", "user"]);
    }

    /// <summary>A malformed path names its index and throws before the JSON is parsed, so a malformed body does not
    /// hide it.</summary>
    [Test]
    public async Task MalformedPath_ThrowsNamingItsIndexBeforeTheJsonIsRead(CancellationToken ct)
    {
        using var response = Buffered("not json");

        var onString = await Assert.That(async () => await Assert.That("not json").HasJsonProperties(["user", "a..b"]))
            .Throws<ArgumentException>();
        await Assert.That(async () => await Assert.That(response).HasJsonProperties(["items[x]"], ct))
            .Throws<ArgumentException>().WithMessageContaining("Path 'items[x]' (paths[0]) has the index '[x]'", StringComparison.Ordinal);

        await Assert.That(onString!.Message).StartsWith("Path 'a..b' (paths[1]) has an empty segment at character 3");
        await Assert.That(onString.ParamName).IsEqualTo("paths");
    }

    [Test]
    public async Task MalformedBody_FailsWithTheParseFailure(CancellationToken ct)
    {
        using var response = Buffered("{\"user\":");

        var ex = await Assert.That(async () => await Assert.That(response).HasJsonProperties(["user"], ct))
            .Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("but the value is not valid JSON: ");
    }

    [Test]
    public async Task DefaultElement_Fails(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var ex = await Assert.That(async () => await Assert.That(default(JsonElement)).HasJsonProperties(["user"]))
            .Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("the JsonElement is default");
    }
}
