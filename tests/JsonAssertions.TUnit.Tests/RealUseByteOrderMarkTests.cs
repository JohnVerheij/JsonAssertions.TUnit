using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// A JSON string that starts with a byte order mark, as <see cref="Encoding.GetString(byte[])"/> returns it for
/// UTF-8 bytes with a BOM (a response body read with <c>ReadAsStringAsync</c> has it removed already). RFC 8259 lets a
/// parser ignore the mark; every string input of the package does.
/// </summary>
[Timeout(10_000)]
internal sealed class RealUseByteOrderMarkTests
{
    private static readonly string WithByteOrderMark = Encoding.UTF8.GetString([0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes(RealUseTestSupport.Body)]);

    [Test]
    public async Task StringWithAByteOrderMark_IsAsserted(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await Assert.That(WithByteOrderMark[0]).IsEqualTo('\uFEFF');
        await Assert.That(WithByteOrderMark).HasJsonValue("user.name", "alice");
        await Assert.That(WithByteOrderMark).ContainsJson("""{"user":{"age":31}}""");
    }

    [Test]
    public async Task ExpectedDocumentWithAByteOrderMark_IsCompared(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await Assert.That(RealUseTestSupport.Body).IsEquivalentJsonTo(WithByteOrderMark);
        await Assert.That(RealUseTestSupport.Body).ContainsJson(WithByteOrderMark);
    }

    [Test]
    public async Task StringWithAByteOrderMark_IsExtractedAndCanonicalized(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await Assert.That(WithByteOrderMark.GetJsonValue<int>("user.age")).IsEqualTo(31);
        await Assert.That(JsonCanonicalizer.Canonicalize(WithByteOrderMark)).IsEqualTo(JsonCanonicalizer.Canonicalize(RealUseTestSupport.Body));
    }
}
