using System;
using System.Text.Json;
using JsonAssertions;
using TUnit.Assertions.Core;

namespace JsonAssertions.TUnit;

/// <summary>
/// Shared parse-guard for the JSON-<see cref="string"/> assertion overloads. Parses the JSON
/// text into a disposable <see cref="JsonDocument"/> and runs the supplied assertion against
/// its root element <em>within</em> the document's lifetime. A parse failure is mapped to a
/// failed <see cref="AssertionResult"/> rather than allowed to escape as a raw
/// <see cref="JsonException"/>: a malformed response body is a legitimate runtime scenario,
/// and the whole point of the package is that a failing assertion explains itself.
/// </summary>
internal static class JsonStringSource
{
    /// <summary>
    /// Parses <paramref name="json"/> and invokes <paramref name="assert"/> against the root
    /// element. Returns the assertion's result on success, or a failed result describing the
    /// parse error if <paramref name="json"/> is not valid JSON.
    /// </summary>
    public static AssertionResult Assert(string json, Func<JsonElement, AssertionResult> assert)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException exception)
        {
            return AssertionResult.Failed(JsonFailureMessage.ParseFailure(exception));
        }

        using (document)
        {
            return assert(document.RootElement);
        }
    }
}
