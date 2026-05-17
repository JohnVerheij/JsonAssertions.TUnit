using System;
using System.Text.Json.Serialization;
using TUnit.Assertions.Core;

namespace JsonAssertions.TUnit;

/// <summary>
/// Internal implementation of <see cref="IJsonContextAssertionSource"/>. Wraps an arbitrary
/// <see cref="IAssertionSource{TContext}"/> whose <typeparamref name="TContext"/> is a
/// <see cref="JsonSerializerContext"/> subtype and exposes its assertion context viewed at the
/// base type. The upcast goes through TUnit's existing <see cref="AssertionContext{TValue}.Map{TNew}(System.Func{TValue, TNew})"/>
/// pipeline; no reflection, no AOT-incompatible patterns.
/// </summary>
/// <typeparam name="TContext">The concrete <see cref="JsonSerializerContext"/> subtype the consumer asserted against.</typeparam>
internal sealed class JsonContextAssertionSourceAdapter<TContext> : IJsonContextAssertionSource
    where TContext : JsonSerializerContext
{
    private readonly AssertionContext<JsonSerializerContext> _context;

    /// <summary>Wraps an existing TUnit assertion source and prepares the upcast assertion context.</summary>
    /// <param name="source">The TUnit assertion source backing this adapter.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    public JsonContextAssertionSourceAdapter(IAssertionSource<TContext> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        // Map performs an implicit reference upcast (TContext : JsonSerializerContext); safe and AOT-clean.
        _context = source.Context.Map<JsonSerializerContext>(static x => x);
    }

    /// <inheritdoc />
    public AssertionContext<JsonSerializerContext> Context => _context;
}
