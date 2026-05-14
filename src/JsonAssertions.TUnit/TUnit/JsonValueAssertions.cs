using System.Globalization;
using System.Text.Json;
using JsonAssertions;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace JsonAssertions.TUnit;

/// <summary>
/// Fluent TUnit entry points for asserting the value at a dot-separated JSON path. Each method
/// is generated into a TUnit assertion chain via the <c>[GenerateAssertion]</c> source
/// generator and delegates to <see cref="JsonPath"/> and <see cref="JsonValueComparison"/> in
/// the framework-agnostic core. Both a JSON <see cref="string"/> and a <see cref="JsonElement"/>
/// are accepted as the asserted value; the expected value may be a <see cref="string"/>,
/// a <see cref="bool"/>, or a number (passed as <see cref="double"/>; <see cref="int"/> and
/// <see cref="long"/> literals widen at the call site).
/// </summary>
/// <remarks>
/// The methods return <see cref="AssertionResult"/> so a failure surfaces either where the
/// path resolution stopped, or the value that was found in place of the expected one (via
/// <see cref="JsonFailureMessage"/>). The <see cref="string"/> overloads parse into a
/// disposable <see cref="JsonDocument"/> and delegate to the <see cref="JsonElement"/> overload
/// <em>within</em> the document's lifetime, because a <see cref="JsonElement"/> is only valid
/// while its backing document is alive.
/// </remarks>
public static class JsonValueAssertions
{
    /// <summary>Asserts the JSON string has the string value <paramref name="expected"/> at
    /// the dot-separated <paramref name="path"/>.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.name</c>.</param>
    /// <param name="expected">The expected string value.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonValue(this string json, string path, string expected)
    {
        using var document = JsonDocument.Parse(json);
        return HasJsonValue(document.RootElement, path, expected);
    }

    /// <summary>Asserts the JSON element has the string value <paramref name="expected"/> at
    /// the dot-separated <paramref name="path"/>.</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.name</c>.</param>
    /// <param name="expected">The expected string value.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonValue(this JsonElement element, string path, string expected)
    {
        var resolution = JsonPath.Resolve(element, path);
        return resolution.Found && JsonValueComparison.Matches(resolution.Element, expected)
            ? AssertionResult.Passed
            : AssertionResult.Failed(JsonFailureMessage.ValueMismatch(path, resolution, "\"" + expected + "\""));
    }

    /// <summary>Asserts the JSON string has the boolean value <paramref name="expected"/> at
    /// the dot-separated <paramref name="path"/>.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.active</c>.</param>
    /// <param name="expected">The expected boolean value.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonValue(this string json, string path, bool expected)
    {
        using var document = JsonDocument.Parse(json);
        return HasJsonValue(document.RootElement, path, expected);
    }

    /// <summary>Asserts the JSON element has the boolean value <paramref name="expected"/> at
    /// the dot-separated <paramref name="path"/>.</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.active</c>.</param>
    /// <param name="expected">The expected boolean value.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonValue(this JsonElement element, string path, bool expected)
    {
        var resolution = JsonPath.Resolve(element, path);
        if (resolution.Found && JsonValueComparison.Matches(resolution.Element, expected))
        {
            return AssertionResult.Passed;
        }

        var expectedDescription = expected ? "true" : "false";
        return AssertionResult.Failed(JsonFailureMessage.ValueMismatch(path, resolution, expectedDescription));
    }

    /// <summary>Asserts the JSON string has the numeric value <paramref name="expected"/> at
    /// the dot-separated <paramref name="path"/>.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.age</c>.</param>
    /// <param name="expected">The expected numeric value.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonValue(this string json, string path, double expected)
    {
        using var document = JsonDocument.Parse(json);
        return HasJsonValue(document.RootElement, path, expected);
    }

    /// <summary>Asserts the JSON element has the numeric value <paramref name="expected"/> at
    /// the dot-separated <paramref name="path"/>.</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.age</c>.</param>
    /// <param name="expected">The expected numeric value.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonValue(this JsonElement element, string path, double expected)
    {
        var resolution = JsonPath.Resolve(element, path);
        return resolution.Found && JsonValueComparison.Matches(resolution.Element, expected)
            ? AssertionResult.Passed
            : AssertionResult.Failed(JsonFailureMessage.ValueMismatch(
                path, resolution, expected.ToString(CultureInfo.InvariantCulture)));
    }
}
