using System;
using System.Text.Json;
using JsonAssertions;
using TUnit.Assertions.Core;

namespace JsonAssertions.TUnit;

/// <summary>
/// Shared guard for the <see cref="JsonElement"/> assertion overloads. A <see langword="default"/> element (for example
/// a field that was never assigned) holds no JSON value and no document, so no assertion may pass on it: a negative
/// assertion such as <c>DoesNotHaveJsonProperty</c> would otherwise pass vacuously.
/// </summary>
internal static class JsonElementSource
{
    /// <summary>
    /// Runs <paramref name="assert"/> against <paramref name="element"/>, or fails when the element is
    /// <see langword="default"/>.
    /// </summary>
    /// <param name="element">The asserted element.</param>
    /// <param name="assert">The assertion to run on an element that holds a value.</param>
    /// <returns>The assertion's result, or a failed result for a <see langword="default"/> element.</returns>
    public static AssertionResult Assert(JsonElement element, Func<JsonElement, AssertionResult> assert)
        => element.ValueKind is JsonValueKind.Undefined
            ? AssertionResult.Failed(JsonFailureMessage.DefaultElement)
            : assert(element);
}
