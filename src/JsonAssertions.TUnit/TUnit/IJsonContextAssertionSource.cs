using System.Text.Json.Serialization;
using TUnit.Assertions.Core;

namespace JsonAssertions.TUnit;

/// <summary>
/// Covariant-style bridge interface used by JSON-context assertions to escape the C# partial
/// generic inference limit when the underlying assertion-source generic is a closed subtype of
/// <see cref="JsonSerializerContext"/>.
/// </summary>
/// <remarks>
/// <para>The TUnit invariant <c>IAssertionSource&lt;TContext&gt;</c> binds at the concrete subtype
/// of <see cref="JsonSerializerContext"/> the consumer wrote, e.g. <c>IAssertionSource&lt;MyContext&gt;</c>.
/// A natural assertion like <c>HasJsonTypeInfoFor&lt;MyDto&gt;()</c> would force the consumer to
/// specify both type arguments at the call site (<c>HasJsonTypeInfoFor&lt;MyDto, MyContext&gt;()</c>)
/// because C# does not allow partial inference: either all method-level type arguments are
/// supplied or none are.</para>
/// <para>This interface is the receiver type for the JSON-context assertion family. The bridge
/// extension <see cref="JsonContextAssertions.AsJsonContext{TContext}(IAssertionSource{TContext})"/>
/// wraps an invariant <c>IAssertionSource&lt;TContext&gt;</c> into an <see cref="IJsonContextAssertionSource"/>
/// whose <see cref="Context"/> is statically typed at <see cref="JsonSerializerContext"/>. The
/// upcast is performed at construction time via the existing TUnit context mapping; no
/// reflection is used and the path stays AOT-clean.</para>
/// <para>Call site shape consumers see:
/// <code>await Assert.That(MyContext.Default).AsJsonContext().HasJsonTypeInfoFor&lt;MyDto&gt;();</code></para>
/// </remarks>
public interface IJsonContextAssertionSource
{
    /// <summary>
    /// The TUnit assertion context viewed at the <see cref="JsonSerializerContext"/> base type.
    /// All JSON-context-family assertions execute against this context.
    /// </summary>
    AssertionContext<JsonSerializerContext> Context { get; }
}
