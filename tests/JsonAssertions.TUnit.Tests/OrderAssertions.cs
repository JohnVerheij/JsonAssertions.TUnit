using System.Text.Json;
using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// The README's consumer-authored assertion, verbatim: a domain assertion built on the public <see cref="JsonPath"/>
/// and <see cref="JsonFailureMessage"/> surface, whose failure reads like the package's own.
/// <c>FullMessagePinTests.ConsumerExtensionSample</c> pins its text.
/// </summary>
internal static class OrderAssertions
{
    [GenerateAssertion(ExpectationMessage = "to have order status '{expectedStatus}'")]
    public static AssertionResult HasOrderStatusOf(this string body, string expectedStatus)
    {
        using var doc = JsonDocument.Parse(body);
        var resolution = JsonPath.Resolve(doc.RootElement, "order.status");
        if (!resolution.Found)
        {
            return AssertionResult.Failed(JsonFailureMessage.PropertyNotFound("order.status", resolution));
        }

        return JsonValueComparison.Matches(resolution.Element, expectedStatus)
            ? AssertionResult.Passed
            : AssertionResult.Failed(JsonFailureMessage.ValueMismatch("order.status", resolution, expectedDescription: string.Empty));
    }
}
