using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace JsonAssertions;

/// <summary>
/// Mirror of the ASP.NET Core ValidationProblemDetails wire shape: a ProblemDetails with an
/// additional <c>errors</c> dictionary mapping field names to validation-error messages.
/// </summary>
/// <remarks>
/// Same MIT-posture rationale as <see cref="ProblemDetailsMirror"/>. The <c>errors</c>
/// property is asserted by <c>MatchesValidationProblemDetails</c> separately from the
/// base-class fields.
/// </remarks>
[SuppressMessage(
    "MeziantouAnalyzer",
    "MA0182:Unused internal type",
    Justification = "Consumed by the MatchesValidationProblemDetails assertion path and by the internal STJ source-gen context in the same assembly.")]
internal sealed class ValidationProblemDetailsMirror : ProblemDetailsMirror
{
    /// <summary>The validation errors, keyed by field name (ASP.NET Core convention).</summary>
    /// <remarks>Uses <c>get; set;</c> to match <see cref="ProblemDetailsMirror"/>'s accessor
    /// pattern. See that type's remarks for why <c>init</c> is avoided.</remarks>
    [JsonPropertyName("errors")]
    public Dictionary<string, string[]>? Errors { get; set; }
}
