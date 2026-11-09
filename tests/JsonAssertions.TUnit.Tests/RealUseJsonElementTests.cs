using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TUnit.Assertions.Exceptions;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// <see cref="JsonElement"/> values a test can hold by mistake: a <see langword="default"/> element (a field that was
/// never assigned) and an element whose <see cref="JsonDocument"/> is already disposed. A default element holds no
/// JSON at all, so no assertion may pass on it.
/// </summary>
[Timeout(10_000)]
internal sealed class RealUseJsonElementTests
{
    private static JsonElement Unassigned => default;

    [Test]
    public async Task DefaultElement_DoesNotSatisfyDoesNotHaveJsonProperty(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var ex = await Assert.That(async () => await Assert.That(Unassigned).DoesNotHaveJsonProperty("password"))
            .Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("default");
    }

    [Test]
    public async Task DefaultElement_DoesNotSatisfyARootAssertion(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await Assert.That(async () => await Assert.That(Unassigned).HasJsonProperty("$")).Throws<AssertionException>();
        await Assert.That(async () => await Assert.That(Unassigned).HasJsonValueMatching("$", static _ => true))
            .Throws<AssertionException>();
    }

    /// <summary>On 0.6.2 the failure renders the default element as <c>null</c>, so "expected: null / actual: null" is
    /// reported as a kind difference.</summary>
    [Test]
    public async Task DefaultElement_EquivalenceFailure_SaysTheElementIsDefault(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var ex = await Assert.That(async () => await Assert.That(Unassigned).IsEquivalentJsonTo("null"))
            .Throws<AssertionException>();

        await Assert.That(ex!.Message).Contains("default");
        await Assert.That(ex.Message).DoesNotContain("actual:   null");
    }

    [Test]
    public async Task DefaultElement_Extraction_ThrowsJsonExtractionException(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await Assert.That(() => Unassigned.GetJsonElement("$")).Throws<JsonExtractionException>();
        await Assert.That(() => Unassigned.GetJsonValue<int>("a")).Throws<JsonExtractionException>()
            .WithMessageContaining("default", StringComparison.Ordinal);
    }

    /// <summary>The two common ways a test builds an element that outlives any document: serializing a value with
    /// <see cref="JsonSerializer.SerializeToElement{TValue}(TValue, System.Text.Json.Serialization.Metadata.JsonTypeInfo{TValue})"/>,
    /// and <see cref="JsonElement.Clone"/> of a parsed element whose document is then disposed.</summary>
    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task SerializedOrClonedElement_IsAssertedByPathValueAndEquivalence(bool serialized, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        JsonElement element;
        if (serialized)
        {
            element = JsonSerializer.SerializeToElement(new HasJsonResponseTests.TestDto(7, "alice"), HasJsonResponseTests.TestJsonContext.Default.TestDto);
        }
        else
        {
            using var document = JsonDocument.Parse("""{"Id":7,"Name":"alice"}""");
            element = document.RootElement.Clone();
        }

        await Assert.That(element).HasJsonProperty("Name");
        await Assert.That(element).HasJsonValue("Id", 7);
        await Assert.That(element).IsEquivalentJsonTo("""{"Name":"alice","Id":7}""");
        await Assert.That(element.GetJsonValue<int>("Id")).IsEqualTo(7);
    }

    [Test]
    public async Task ElementOfADisposedDocument_ThrowsObjectDisposedException(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        JsonElement stale;
        using (var document = JsonDocument.Parse(RealUseTestSupport.Body))
        {
            stale = document.RootElement;
        }

        await Assert.That(async () => await Assert.That(stale).HasJsonProperty("user")).Throws<ObjectDisposedException>();
        await Assert.That(() => stale.GetJsonValue<int>("user.age")).Throws<ObjectDisposedException>();
    }
}
