using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using JsonAssertions;
using TUnit.Assertions.Core;

namespace JsonAssertions.TUnit;

/// <summary>
/// Fluent TUnit entry points for asserting the registration state of a
/// <see cref="JsonSerializerContext"/>. AOT-clean (the context's own type registry is the
/// authoritative source; no reflection over the asserted type needed) and STJ-shaped.
/// </summary>
/// <remarks>
/// <para>The headline assertion is <see cref="HasJsonTypeInfoFor{T}(IJsonContextAssertionSource)"/>:
/// it catches the "added a property / new domain type but forgot to add
/// <c>[JsonSerializable(typeof(NewType))]</c> to the context" regression class. Each
/// production <c>JsonSerializerContext</c> in a consumer codebase wants a one-line
/// regression test per registered type; the assertion makes that test trivial.</para>
/// <para>This is the educational-demand AOT-moat companion to <c>RoundtripsCleanlyVia</c>:
/// where <c>RoundtripsCleanlyVia</c> verifies a value round-trips cleanly through a typed
/// context, <c>HasJsonTypeInfoFor</c> verifies the context KNOWS about the type at all.
/// A consumer that adopts both gets a complete "my serializer context is in sync with my
/// domain types" CI gate.</para>
/// <para>Consumers chain the bridge extension <see cref="AsJsonContext{TContext}(IAssertionSource{TContext})"/>
/// before the leaf assertion to keep the call site to a single explicit type argument:
/// <code>await Assert.That(MyContext.Default).AsJsonContext().HasJsonTypeInfoFor&lt;MyDto&gt;();</code>
/// See <see cref="IJsonContextAssertionSource"/> for the technical rationale behind the bridge step.</para>
/// </remarks>
public static class JsonContextAssertions
{
    /// <summary>
    /// Bridges an invariant TUnit assertion source typed at a concrete
    /// <see cref="JsonSerializerContext"/> subtype into the JSON-context assertion family by
    /// returning an <see cref="IJsonContextAssertionSource"/> whose <see cref="IJsonContextAssertionSource.Context"/>
    /// is typed at <see cref="JsonSerializerContext"/>. The single method-level generic
    /// <typeparamref name="TContext"/> is inferred from the assertion source, so the call site
    /// reads <c>Assert.That(MyContext.Default).AsJsonContext()</c> without explicit type
    /// arguments.
    /// </summary>
    /// <typeparam name="TContext">The concrete <see cref="JsonSerializerContext"/> subtype, inferred from <paramref name="source"/>.</typeparam>
    /// <param name="source">The TUnit assertion source produced by <c>Assert.That(myContext)</c>.</param>
    /// <returns>A JSON-context assertion source ready for the family's fluent assertions.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public static IJsonContextAssertionSource AsJsonContext<TContext>(this IAssertionSource<TContext> source)
        where TContext : JsonSerializerContext
    {
        ArgumentNullException.ThrowIfNull(source);
        return new JsonContextAssertionSourceAdapter<TContext>(source);
    }

    /// <summary>Asserts the underlying <see cref="JsonSerializerContext"/> has a
    /// <see cref="System.Text.Json.Serialization.Metadata.JsonTypeInfo{T}"/> registered for
    /// <typeparamref name="T"/>. Catches the common "added a property / new domain type but
    /// forgot to add <c>[JsonSerializable(typeof(NewType))]</c> to the context" regression
    /// class before any runtime serialization touches the unregistered type.</summary>
    /// <typeparam name="T">The type whose registration is asserted.</typeparam>
    /// <param name="source">A JSON-context assertion source obtained via <see cref="AsJsonContext{TContext}(IAssertionSource{TContext})"/>.</param>
    /// <returns>An awaitable <see cref="HasJsonTypeInfoForAssertion{T}"/> that resolves to passed when the context knows about <typeparamref name="T"/>; otherwise a failed assertion identifying the missing type and context.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    [SuppressMessage(
        "Usage",
        "VSTHRD200:Use \"Async\" suffix for async methods",
        Justification = "Fluent assertion entry point; an Async suffix would corrupt the chain surface. The returned assertion is awaitable.")]
    public static HasJsonTypeInfoForAssertion<T> HasJsonTypeInfoFor<T>(this IJsonContextAssertionSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        source.Context.ExpressionBuilder.Append(".HasJsonTypeInfoFor()");
        return new HasJsonTypeInfoForAssertion<T>(source.Context);
    }
}
