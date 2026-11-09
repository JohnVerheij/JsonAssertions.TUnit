using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using JsonAssertions;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace JsonAssertions.TUnit;

/// <summary>
/// Fluent TUnit entry points for asserting the presence or absence of a property at a
/// dot-separated JSON path. Each method is generated into a TUnit assertion chain via the
/// <c>[GenerateAssertion]</c> source generator and delegates to <see cref="JsonPath"/> in the
/// framework-agnostic core. Both a JSON <see cref="string"/> and a <see cref="JsonElement"/>
/// are accepted as the asserted value.
/// </summary>
/// <remarks>
/// The methods return <see cref="AssertionResult"/> so a failure surfaces <em>where</em> on
/// the path resolution stopped (via <see cref="JsonFailureMessage"/>), not merely that it did.
/// The <see cref="string"/> overloads parse into a disposable <see cref="JsonDocument"/> and
/// delegate to the <see cref="JsonElement"/> overload <em>within</em> the document's lifetime:
/// a <see cref="JsonElement"/> is only valid while its backing document is alive, and the
/// returned <see cref="AssertionResult"/> carries only the already-rendered text.
/// </remarks>
[SuppressMessage(
    "Performance",
    "MA0109:Consider adding an overload with a Span<T> or Memory<T>",
    Justification = "HasJsonProperties takes string[] so callers can use a C# 12 collection expression literal (HasJsonProperties([\"user.name\", \"items[*].id\"])); a ReadOnlySpan<T> overload cannot be expressed under TUnit's [GenerateAssertion] source generator (ref struct parameters are unsupported), and assertion call sites do not need the allocation profile a Span overload would provide.")]
public static class JsonPropertyAssertions
{

    /// <summary>Asserts the JSON string has a property at the dot-separated
    /// <paramref name="path"/>.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.address.city</c>.</param>
    [GenerateAssertion(ExpectationMessage = Expectations.HasJsonProperty)]
    public static AssertionResult HasJsonProperty(this string json, string path)
        => JsonStringSource.Assert(json, root => HasJsonProperty(root, path));

    /// <summary>Asserts the JSON element has a property at the dot-separated
    /// <paramref name="path"/>.</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.address.city</c>.</param>
    [GenerateAssertion(ExpectationMessage = Expectations.HasJsonProperty)]
    public static AssertionResult HasJsonProperty(this JsonElement element, string path)
        => JsonElementSource.Assert(element, root => HasJsonPropertyAt(root, path));

    /// <summary>Asserts the JSON string has a property at each of <paramref name="paths"/>, parsing it once. The failure
    /// lists every path that does not resolve, in the given order.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="paths">The paths, each as in <see cref="HasJsonProperty(string, string)"/>, the <c>[*]</c> wildcard
    /// included; at least one, with no repeats, for example <c>HasJsonProperties(["user.name", "items[*].id"])</c>.</param>
    /// <exception cref="System.ArgumentException"><paramref name="paths"/> is empty, or a path is
    /// <see langword="null"/>, blank, malformed or repeated; checked before the JSON is parsed.</exception>
    [GenerateAssertion(ExpectationMessage = Expectations.HasJsonProperties)]
    public static AssertionResult HasJsonProperties(this string json, string[] paths)
    {
        JsonPath.ValidateAll(paths);
        return JsonStringSource.Assert(json, root => HasJsonPropertiesAt(root, paths));
    }

    /// <summary>Asserts the JSON element has a property at each of <paramref name="paths"/>. The failure lists every
    /// path that does not resolve, in the given order.</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="paths">The paths, each as in <see cref="HasJsonProperty(JsonElement, string)"/>, the <c>[*]</c>
    /// wildcard included; at least one, with no repeats.</param>
    /// <exception cref="System.ArgumentException"><paramref name="paths"/> is empty, or a path is
    /// <see langword="null"/>, blank, malformed or repeated.</exception>
    [GenerateAssertion(ExpectationMessage = Expectations.HasJsonProperties)]
    public static AssertionResult HasJsonProperties(this JsonElement element, string[] paths)
    {
        JsonPath.ValidateAll(paths);
        return JsonElementSource.Assert(element, root => HasJsonPropertiesAt(root, paths));
    }

    /// <summary>Asserts a property exists at each of the checked <paramref name="paths"/> on an element that holds a
    /// value. A <c>[*]</c> path needs every element it expands to; over an empty array it passes.</summary>
    private static AssertionResult HasJsonPropertiesAt(JsonElement element, string[] paths)
    {
        var failures = new List<(string Path, JsonPathResolution Failure, int Failed, int Total)>();
        foreach (var path in paths)
        {
            var resolutions = JsonPath.ResolveAll(element, path);
            var failed = resolutions.Where(static resolution => !resolution.Found).ToList();
            if (failed.Count > 0)
            {
                failures.Add((path, failed[0], failed.Count, resolutions.Count));
            }
        }

        return failures.Count is 0
            ? AssertionResult.Passed
            : AssertionResult.Failed(JsonFailureMessage.PropertiesNotFound(failures, paths.Length));
    }

    /// <summary>Asserts a property exists at <paramref name="path"/> on an element that holds a value.</summary>
    private static AssertionResult HasJsonPropertyAt(JsonElement element, string path)
    {
        if (JsonPath.ContainsWildcard(path))
        {
            // A `[*]` path requires every expanded element to resolve; report the first that does
            // not (its prefix names which element, e.g. `[1]`). An empty array expands to zero
            // resolutions and passes vacuously.
            foreach (var expanded in JsonPath.ResolveAll(element, path))
            {
                if (!expanded.Found)
                {
                    return AssertionResult.Failed(JsonFailureMessage.PropertyNotFound(path, expanded));
                }
            }

            return AssertionResult.Passed;
        }

        var resolution = JsonPath.Resolve(element, path);
        return resolution.Found
            ? AssertionResult.Passed
            : AssertionResult.Failed(JsonFailureMessage.PropertyNotFound(path, resolution));
    }

    /// <summary>Asserts the JSON string has no property at the dot-separated
    /// <paramref name="path"/>. A malformed JSON string fails the assertion (a body that
    /// cannot be parsed must not vacuously satisfy a negative assertion).</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.address.city</c>.
    /// A malformed path, or one with the <c>[*]</c> wildcard, throws <see cref="System.ArgumentException"/> before
    /// any navigation: a wildcard has no single location to be absent from.</param>
    [GenerateAssertion(ExpectationMessage = Expectations.DoesNotHaveJsonProperty)]
    public static AssertionResult DoesNotHaveJsonProperty(this string json, string path)
        => JsonStringSource.Assert(json, root => DoesNotHaveJsonProperty(root, path));

    /// <summary>Asserts the JSON element has no property at the dot-separated
    /// <paramref name="path"/>.</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.address.city</c>.
    /// A malformed path, or one with the <c>[*]</c> wildcard, throws <see cref="System.ArgumentException"/> before
    /// any navigation: a wildcard has no single location to be absent from.</param>
    [GenerateAssertion(ExpectationMessage = Expectations.DoesNotHaveJsonProperty)]
    public static AssertionResult DoesNotHaveJsonProperty(this JsonElement element, string path)
        => JsonElementSource.Assert(element, root =>
        {
            var resolution = JsonPath.Resolve(root, path);
            return resolution.Found
                ? AssertionResult.Failed(JsonFailureMessage.PropertyShouldNotExist(path, resolution))
                : AssertionResult.Passed;
        });
}
