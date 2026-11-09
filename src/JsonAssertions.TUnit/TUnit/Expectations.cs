namespace JsonAssertions.TUnit;

/// <summary>
/// The expectation texts of the generated assertions, shared by the <see cref="string"/>,
/// <see cref="System.Text.Json.JsonElement"/> and <see cref="System.Net.Http.HttpResponseMessage"/> forms of each, so
/// TUnit prints <c>Expected to have a JSON property at path 'user.email'</c> whatever the receiver. TUnit's generator
/// pastes a text into an interpolated string and fills <c>{name}</c> with that parameter's value, so a text has no
/// double quote (a string placeholder sits in single quotes) and names only parameters that render well.
/// </summary>
internal static class Expectations
{
    /// <summary>The expectation of <c>HasJsonProperty</c>.</summary>
    internal const string HasJsonProperty = "to have a JSON property at path '{path}'";

    /// <summary>The expectation of <c>HasJsonProperties</c>.</summary>
    internal const string HasJsonProperties = "to have a JSON property at each of the given paths";

    /// <summary>The expectation of <c>DoesNotHaveJsonProperty</c>.</summary>
    internal const string DoesNotHaveJsonProperty = "to have no JSON property at path '{path}'";

    /// <summary>The expectation of <c>HasJsonValue</c> with a <see cref="string"/>.</summary>
    internal const string HasJsonString = "to have the JSON string '{expected}' at path '{path}'";

    /// <summary>The expectation of <c>HasJsonValue</c> with a <see cref="bool"/>, which the generator would print as
    /// <c>True</c>; the failure gives the value.</summary>
    internal const string HasJsonBooleanValue = "to have the given JSON boolean at path '{path}'";

    /// <summary>The expectation of <c>HasJsonValue</c> with a <see cref="double"/>, which the generator would print in
    /// the current culture; the failure gives the value.</summary>
    internal const string HasJsonNumber = "to have the given JSON number at path '{path}'";

    /// <summary>The expectation of <c>HasJsonValue</c> with an integer.</summary>
    internal const string HasJsonInteger = "to have the JSON integer {expected} at path '{path}'";

    /// <summary>The expectation of <c>HasJsonValueMatching</c>.</summary>
    internal const string HasJsonValueMatching = "to have a value matching the predicate at path '{path}'";

    /// <summary>The expectation of <c>HasJsonValueOneOf</c>; the failure lists the candidates.</summary>
    internal const string HasJsonValueOneOf = "to have one of the given values at path '{path}'";

    /// <summary>The expectation of <c>HasJsonValueParsableAs</c>; the failure names the type.</summary>
    internal const string HasJsonValueParsableAs = "to have a JSON string parseable as the given type at path '{path}'";

    /// <summary>The expectation of <c>HasJsonValueKind</c>.</summary>
    internal const string HasJsonValueKind = "to have a JSON value of kind {expectedKind} at path '{path}'";

    /// <summary>The expectation of <c>HasJsonBoolean</c>.</summary>
    internal const string HasJsonBoolean = "to have a JSON boolean at path '{path}'";

    /// <summary>The expectation of <c>HasNonEmptyJsonString</c>.</summary>
    internal const string HasNonEmptyJsonString = "to have a non-empty JSON string at path '{path}'";

    /// <summary>The expectation of <c>HasJsonArrayLength</c>.</summary>
    internal const string HasJsonArrayLength = "to have a JSON array of length {expectedLength} at path '{path}'";

    /// <summary>The expectation of <c>HasNonEmptyJsonArray</c>.</summary>
    internal const string HasNonEmptyJsonArray = "to have a non-empty JSON array at path '{path}'";

    /// <summary>The expectation of <c>HasEmptyJsonArray</c>.</summary>
    internal const string HasEmptyJsonArray = "to have an empty JSON array at path '{path}'";

    /// <summary>The expectation of <c>IsEquivalentJsonTo</c>.</summary>
    internal const string IsEquivalentJsonTo = "to be JSON-equivalent to the expected document";

    /// <summary>The expectation of <c>ContainsJson</c>.</summary>
    internal const string ContainsJson = "to contain the expected JSON";

    /// <summary>The expectation of <c>HasJsonResponse</c>.</summary>
    internal const string HasJsonResponse = "to have status {expectedStatus} and a JSON body equal to {expected}";

    /// <summary>The expectation of <c>MatchesProblemDetails</c>.</summary>
    internal const string MatchesProblemDetails = "to be an RFC 7807 problem response with status {status}";

    /// <summary>The expectation of <c>MatchesValidationProblemDetails</c>.</summary>
    internal const string MatchesValidationProblemDetails =
        "to be an RFC 7807 validation problem response with status {status} and the expected errors";

    /// <summary>The expectation of <c>RoundtripsCleanlyVia</c>.</summary>
    internal const string RoundtripsCleanlyVia = "to serialize the same after a round trip through its JsonTypeInfo";
}
