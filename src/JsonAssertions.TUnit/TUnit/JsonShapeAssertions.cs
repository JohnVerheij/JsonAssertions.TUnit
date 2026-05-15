using System.Globalization;
using System.Text.Json;
using JsonAssertions;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace JsonAssertions.TUnit;

/// <summary>
/// Fluent TUnit entry points for asserting the <em>shape</em> of the value at a dot-separated
/// JSON path: that it is a JSON array of a given length (or non-empty, or empty), or that it
/// is of a given <see cref="JsonValueKind"/>. Each method is generated into a TUnit assertion
/// chain via the <c>[GenerateAssertion]</c> source generator and delegates to
/// <see cref="JsonPath"/> and <see cref="JsonShape"/> in the framework-agnostic core. Both a
/// JSON <see cref="string"/> and a <see cref="JsonElement"/> are accepted as the asserted
/// value.
/// </summary>
/// <remarks>
/// The methods return <see cref="AssertionResult"/> so a failure surfaces either where the
/// path resolution stopped, or the shape that was found in place of the expected one (via
/// <see cref="JsonFailureMessage"/>).
/// </remarks>
public static class JsonShapeAssertions
{
    /// <summary>Asserts the value at the dot-separated <paramref name="path"/> is a JSON array
    /// with exactly <paramref name="expectedLength"/> elements.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.roles</c>.</param>
    /// <param name="expectedLength">The expected array length.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonArrayLength(this string json, string path, int expectedLength)
        => JsonStringSource.Assert(json, root => HasJsonArrayLength(root, path, expectedLength));

    /// <summary>Asserts the value at the dot-separated <paramref name="path"/> is a JSON array
    /// with exactly <paramref name="expectedLength"/> elements.</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.roles</c>.</param>
    /// <param name="expectedLength">The expected array length.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonArrayLength(this JsonElement element, string path, int expectedLength)
    {
        var resolution = JsonPath.Resolve(element, path);
        return resolution.Found && JsonShape.IsArrayOfLength(resolution.Element, expectedLength)
            ? AssertionResult.Passed
            : AssertionResult.Failed(JsonFailureMessage.ShapeMismatch(
                path, resolution, "a JSON array of length " + expectedLength.ToString(CultureInfo.InvariantCulture)));
    }

    /// <summary>Asserts the value at the dot-separated <paramref name="path"/> is a JSON array
    /// with at least one element.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.roles</c>.</param>
    [GenerateAssertion]
    public static AssertionResult HasNonEmptyJsonArray(this string json, string path)
        => JsonStringSource.Assert(json, root => HasNonEmptyJsonArray(root, path));

    /// <summary>Asserts the value at the dot-separated <paramref name="path"/> is a JSON array
    /// with at least one element.</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.roles</c>.</param>
    [GenerateAssertion]
    public static AssertionResult HasNonEmptyJsonArray(this JsonElement element, string path)
    {
        var resolution = JsonPath.Resolve(element, path);
        return resolution.Found && JsonShape.IsNonEmptyArray(resolution.Element)
            ? AssertionResult.Passed
            : AssertionResult.Failed(JsonFailureMessage.ShapeMismatch(path, resolution, "a non-empty JSON array"));
    }

    /// <summary>Asserts the value at the dot-separated <paramref name="path"/> is a JSON array
    /// with no elements.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.roles</c>.</param>
    [GenerateAssertion]
    public static AssertionResult HasEmptyJsonArray(this string json, string path)
        => JsonStringSource.Assert(json, root => HasEmptyJsonArray(root, path));

    /// <summary>Asserts the value at the dot-separated <paramref name="path"/> is a JSON array
    /// with no elements.</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.roles</c>.</param>
    [GenerateAssertion]
    public static AssertionResult HasEmptyJsonArray(this JsonElement element, string path)
    {
        var resolution = JsonPath.Resolve(element, path);
        return resolution.Found && JsonShape.IsEmptyArray(resolution.Element)
            ? AssertionResult.Passed
            : AssertionResult.Failed(JsonFailureMessage.ShapeMismatch(path, resolution, "an empty JSON array"));
    }

    /// <summary>Asserts the value at the dot-separated <paramref name="path"/> is of the given
    /// <see cref="JsonValueKind"/>.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.meta</c>.</param>
    /// <param name="expectedKind">The expected JSON value kind.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonValueKind(this string json, string path, JsonValueKind expectedKind)
        => JsonStringSource.Assert(json, root => HasJsonValueKind(root, path, expectedKind));

    /// <summary>Asserts the value at the dot-separated <paramref name="path"/> is of the given
    /// <see cref="JsonValueKind"/>.</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A dot-separated property path, for example <c>user.meta</c>.</param>
    /// <param name="expectedKind">The expected JSON value kind.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonValueKind(this JsonElement element, string path, JsonValueKind expectedKind)
    {
        var resolution = JsonPath.Resolve(element, path);
        return resolution.Found && JsonShape.IsKind(resolution.Element, expectedKind)
            ? AssertionResult.Passed
            : AssertionResult.Failed(JsonFailureMessage.ShapeMismatch(
                path, resolution, "a JSON value of kind " + expectedKind));
    }

    /// <summary>Asserts the value at <paramref name="path"/> is a non-empty JSON string.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>user.name</c>.</param>
    [GenerateAssertion]
    public static AssertionResult HasNonEmptyJsonString(this string json, string path)
        => JsonStringSource.Assert(json, root => HasNonEmptyJsonString(root, path));

    /// <summary>Asserts the value at <paramref name="path"/> is a non-empty JSON string.</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>user.name</c>.</param>
    [GenerateAssertion]
    public static AssertionResult HasNonEmptyJsonString(this JsonElement element, string path)
    {
        var resolution = JsonPath.Resolve(element, path);
        return resolution.Found && JsonShape.IsNonEmptyString(resolution.Element)
            ? AssertionResult.Passed
            : AssertionResult.Failed(JsonFailureMessage.ShapeMismatch(path, resolution, "a non-empty JSON string"));
    }

    /// <summary>Asserts the value at <paramref name="path"/> is a JSON boolean (either
    /// <see langword="true"/> or <see langword="false"/>). JSON's <see langword="true"/> and
    /// <see langword="false"/> are distinct <see cref="JsonValueKind"/>s, so this is the
    /// discoverable form of "this field is a boolean, either value" that
    /// <c>HasJsonValueKind</c> alone cannot express.</summary>
    /// <param name="json">The JSON document text.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>user.active</c>.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonBoolean(this string json, string path)
        => JsonStringSource.Assert(json, root => HasJsonBoolean(root, path));

    /// <summary>Asserts the value at <paramref name="path"/> is a JSON boolean (either
    /// <see langword="true"/> or <see langword="false"/>).</summary>
    /// <param name="element">The JSON element to navigate from.</param>
    /// <param name="path">A path of dot-separated property names and zero-based bracket
    /// indices, for example <c>user.active</c>.</param>
    [GenerateAssertion]
    public static AssertionResult HasJsonBoolean(this JsonElement element, string path)
    {
        var resolution = JsonPath.Resolve(element, path);
        return resolution.Found && JsonShape.IsBoolean(resolution.Element)
            ? AssertionResult.Passed
            : AssertionResult.Failed(JsonFailureMessage.ShapeMismatch(path, resolution, "a JSON boolean"));
    }
}
