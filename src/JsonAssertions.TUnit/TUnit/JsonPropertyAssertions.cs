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
public static class JsonPropertyAssertions
{
    /// <summary>Asserts the JSON string has a property at the dot-separated
    /// <paramref name="path"/>.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.address.city</c>.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonProperty(this string json, string path)
        => JsonStringSource.Assert(json, root => HasJsonProperty(root, path));

    /// <summary>Asserts the JSON element has a property at the dot-separated
    /// <paramref name="path"/>.</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.address.city</c>.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonProperty(this JsonElement element, string path)
    {
        var resolution = JsonPath.Resolve(element, path);
        return resolution.Found
            ? AssertionResult.Passed
            : AssertionResult.Failed(JsonFailureMessage.PropertyNotFound(path, resolution));
    }

    /// <summary>Asserts the JSON string has no property at the dot-separated
    /// <paramref name="path"/>. A malformed JSON string fails the assertion (a body that
    /// cannot be parsed must not vacuously satisfy a negative assertion).</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.address.city</c>.</param>
    [GenerateAssertion]
    public static AssertionResult DoesNotHaveJsonProperty(this string json, string path)
        => JsonStringSource.Assert(json, root => DoesNotHaveJsonProperty(root, path));

    /// <summary>Asserts the JSON element has no property at the dot-separated
    /// <paramref name="path"/>.</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.address.city</c>.</param>
    [GenerateAssertion]
    public static AssertionResult DoesNotHaveJsonProperty(this JsonElement element, string path)
    {
        var resolution = JsonPath.Resolve(element, path);
        return resolution.Found
            ? AssertionResult.Failed(JsonFailureMessage.PropertyShouldNotExist(path, resolution))
            : AssertionResult.Passed;
    }
}
