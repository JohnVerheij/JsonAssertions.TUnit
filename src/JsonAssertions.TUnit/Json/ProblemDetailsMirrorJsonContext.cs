using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace JsonAssertions;

/// <summary>
/// STJ source-gen context for the internal ProblemDetails mirrors. Source-gen produces
/// <c>JsonTypeInfo&lt;ProblemDetailsMirror&gt;</c> and
/// <c>JsonTypeInfo&lt;ValidationProblemDetailsMirror&gt;</c> at compile time; the assertion
/// path uses <c>JsonSerializer.Deserialize(json, JsonTypeInfo)</c> for AOT-clean
/// deserialization without runtime reflection.
/// </summary>
[JsonSerializable(typeof(ProblemDetailsMirror))]
[JsonSerializable(typeof(ValidationProblemDetailsMirror))]
[SuppressMessage(
    "MeziantouAnalyzer",
    "MA0182:Unused internal type",
    Justification = "Consumed by the MatchesProblemDetails / MatchesValidationProblemDetails assertion paths.")]
internal sealed partial class ProblemDetailsMirrorJsonContext : JsonSerializerContext;
