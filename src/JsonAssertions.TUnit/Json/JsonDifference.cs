namespace JsonAssertions;

/// <summary>
/// The first structural difference found between two JSON documents by
/// <see cref="JsonEquivalence"/>. Captures the location and a rendered description of each side at
/// the point they diverge, so the difference outlives the <see cref="System.Text.Json.JsonDocument"/>
/// it was found in (a <see cref="System.Text.Json.JsonElement"/> is valid only while its backing
/// document is alive).
/// </summary>
public sealed class JsonDifference
{
    /// <summary>Initializes a difference at <paramref name="path"/>.</summary>
    /// <param name="path">The dot/bracket path where the documents diverge; empty for the root.</param>
    /// <param name="kind">The category of difference.</param>
    /// <param name="expected">A rendered description of the expected side at <paramref name="path"/>.</param>
    /// <param name="actual">A rendered description of the actual side at <paramref name="path"/>.</param>
    public JsonDifference(string path, JsonDifferenceKind kind, string expected, string actual)
    {
        Path = path;
        Kind = kind;
        Expected = expected;
        Actual = actual;
    }

    /// <summary>The dot/bracket path where the documents diverge; empty for the root.</summary>
    public string Path { get; }

    /// <summary>The category of difference.</summary>
    public JsonDifferenceKind Kind { get; }

    /// <summary>A rendered description of the expected side at <see cref="Path"/>.</summary>
    public string Expected { get; }

    /// <summary>A rendered description of the actual side at <see cref="Path"/>.</summary>
    public string Actual { get; }
}
