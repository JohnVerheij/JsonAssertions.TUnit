using System;

namespace JsonAssertions;

/// <summary>
/// Thrown by the <c>GetJsonValue</c> / <c>GetJsonString</c> / <c>GetJsonElement</c> extraction
/// helpers when the requested value cannot be produced: the path does not resolve, the value at the
/// path is the wrong JSON kind, its text does not parse to the requested type, or the source text is
/// not valid JSON. The message carries the same path-context diagnostics the assertions render (how
/// far the path resolved and why it stopped), so an extraction failure explains itself rather than
/// surfacing a bare <see cref="System.Collections.Generic.KeyNotFoundException"/> or
/// <see cref="FormatException"/> from a hand-rolled <c>GetProperty(...).GetInt32()</c> chain.
/// </summary>
public sealed class JsonExtractionException : Exception
{
    /// <summary>Initializes the exception with no message.</summary>
    public JsonExtractionException()
    {
    }

    /// <summary>Initializes the exception with a rendered, path-context failure message.</summary>
    /// <param name="message">The explanatory failure message.</param>
    public JsonExtractionException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes the exception with a message and an inner cause.</summary>
    /// <param name="message">The explanatory failure message.</param>
    /// <param name="innerException">The underlying cause.</param>
    public JsonExtractionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
