using System.Text.Json;
using TUnit.Assertions.Attributes;

namespace JsonAssertions.TUnit;

/// <summary>
/// Fluent TUnit entry points for asserting the presence or absence of a property at a
/// dot-separated JSON path. Each method is generated into a TUnit assertion chain via the
/// <c>[GenerateAssertion]</c> source generator and delegates to
/// <see cref="JsonAssertions.JsonPath"/> in the framework-agnostic core. Both a JSON
/// <see cref="string"/> and a <see cref="JsonElement"/> are accepted as the asserted value.
/// </summary>
public static class JsonPropertyAssertions
{
    /// <summary>Asserts the JSON string has a property at the dot-separated
    /// <paramref name="path"/>.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.address.city</c>.</param>
    [GenerateAssertion(ExpectationMessage = "to have a JSON property at path {path}")]
    public static bool HasJsonProperty(this string json, string path) => ParseAndCheck(json, path);

    /// <summary>Asserts the JSON element has a property at the dot-separated
    /// <paramref name="path"/>.</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.address.city</c>.</param>
    [GenerateAssertion(ExpectationMessage = "to have a JSON property at path {path}")]
    public static bool HasJsonProperty(this JsonElement element, string path)
        => JsonPath.Exists(element, path);

    /// <summary>Asserts the JSON string has no property at the dot-separated
    /// <paramref name="path"/>.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.address.city</c>.</param>
    [GenerateAssertion(ExpectationMessage = "to not have a JSON property at path {path}")]
    public static bool DoesNotHaveJsonProperty(this string json, string path)
        => !ParseAndCheck(json, path);

    /// <summary>Asserts the JSON element has no property at the dot-separated
    /// <paramref name="path"/>.</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.address.city</c>.</param>
    [GenerateAssertion(ExpectationMessage = "to not have a JSON property at path {path}")]
    public static bool DoesNotHaveJsonProperty(this JsonElement element, string path)
        => !JsonPath.Exists(element, path);

    /// <summary>Parses <paramref name="json"/> into a disposable <see cref="JsonDocument"/> and
    /// reports whether a property exists at <paramref name="path"/> within its root element.
    /// </summary>
    private static bool ParseAndCheck(string json, string path)
    {
        using var document = JsonDocument.Parse(json);
        return JsonPath.Exists(document.RootElement, path);
    }
}
