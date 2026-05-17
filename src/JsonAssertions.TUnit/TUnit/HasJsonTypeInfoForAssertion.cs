using System.Text.Json.Serialization;
using System.Threading.Tasks;
using JsonAssertions;
using TUnit.Assertions.Core;

namespace JsonAssertions.TUnit;

/// <summary>The assertion produced by <see cref="JsonContextAssertions.HasJsonTypeInfoFor{T}(IJsonContextAssertionSource)"/>.
/// Holds the type whose registration is checked as a method-level generic and resolves to
/// passed when <see cref="JsonSerializerContext.GetTypeInfo(System.Type)"/> returns a non-null
/// <see cref="System.Text.Json.Serialization.Metadata.JsonTypeInfo{T}"/>.</summary>
/// <typeparam name="T">The type whose registration is asserted.</typeparam>
public sealed class HasJsonTypeInfoForAssertion<T> : Assertion<JsonSerializerContext>
{
    /// <summary>Creates a new assertion bound to the supplied context.</summary>
    /// <param name="context">The TUnit assertion context typed at <see cref="JsonSerializerContext"/>.</param>
    public HasJsonTypeInfoForAssertion(AssertionContext<JsonSerializerContext> context) : base(context)
    {
    }

    /// <inheritdoc />
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<JsonSerializerContext> metadata)
    {
        var value = metadata.Value;
        if (metadata.Exception is not null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {metadata.Exception.GetType().FullName}"));
        }
        if (value is null)
        {
            return Task.FromResult(AssertionResult.Failed("Actual value is null"));
        }
        return Task.FromResult(value.GetTypeInfo(typeof(T)) is not null
            ? AssertionResult.Passed
            : AssertionResult.Failed(JsonFailureMessage.JsonTypeInfoMissing(typeof(T).Name, value.GetType().Name)));
    }

    /// <inheritdoc />
    protected override string GetExpectation() => $"to register JsonTypeInfo<{typeof(T).Name}>";
}
