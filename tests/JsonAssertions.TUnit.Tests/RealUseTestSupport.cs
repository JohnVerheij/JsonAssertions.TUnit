using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JsonAssertions.TUnit.Tests;

/// <summary>
/// Response bodies as a test really meets them: a network-like body that can be read only once, and a body whose
/// read never completes until the token ends it.
/// </summary>
internal static class RealUseTestSupport
{
    /// <summary>A JSON body used by the real-use tests.</summary>
    internal const string Body = """{"user":{"name":"alice","age":31},"items":[{"id":1},{"id":2}]}""";

    /// <summary>A response whose body is <paramref name="body"/> in UTF-8, as a buffered <see cref="StringContent"/>.</summary>
    /// <param name="body">The body.</param>
    /// <param name="status">The status code.</param>
    /// <param name="mediaType">The media type.</param>
    /// <returns>The response; the caller disposes it.</returns>
    internal static HttpResponseMessage Buffered(string body, HttpStatusCode status = HttpStatusCode.OK, string mediaType = "application/json")
        => new(status) { Content = new StringContent(body, Encoding.UTF8, mediaType) };

    /// <summary>A response whose body is <paramref name="bytes"/> behind a stream that cannot seek, like a network body.</summary>
    /// <param name="bytes">The body bytes.</param>
    /// <param name="mediaType">The media type.</param>
    /// <returns>The response; the caller disposes it.</returns>
    internal static HttpResponseMessage OnceReadable(byte[] bytes, string mediaType = "application/json")
    {
        var content = new StreamContent(new OnceReadableStream(bytes));
        content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
    }

    /// <summary>A response with raw body bytes and a full <c>Content-Type</c> header value.</summary>
    /// <param name="bytes">The body bytes.</param>
    /// <param name="contentType">The header value, for example <c>application/json; charset=utf-16</c>.</param>
    /// <param name="status">The status code.</param>
    /// <returns>The response; the caller disposes it.</returns>
    internal static HttpResponseMessage Raw(byte[] bytes, string contentType, HttpStatusCode status = HttpStatusCode.OK)
    {
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        return new HttpResponseMessage(status) { Content = content };
    }

    /// <summary>A forward-only stream over a byte array.</summary>
    internal sealed class OnceReadableStream(byte[] data) : Stream
    {
        private readonly MemoryStream _inner = new(data);

        /// <inheritdoc/>
        public override bool CanRead => true;

        /// <inheritdoc/>
        public override bool CanSeek => false;

        /// <inheritdoc/>
        public override bool CanWrite => false;

        /// <inheritdoc/>
        public override long Length => throw new NotSupportedException();

        /// <inheritdoc/>
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override void Flush()
        {
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

        /// <inheritdoc/>
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => _inner.ReadAsync(buffer, cancellationToken);

        /// <inheritdoc/>
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => _inner.ReadAsync(buffer, offset, count, cancellationToken);

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        /// <inheritdoc/>
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    /// <summary>A stream whose asynchronous read completes only when its token is canceled.</summary>
    internal sealed class NeverEndingStream : Stream
    {
        /// <inheritdoc/>
        public override bool CanRead => true;

        /// <inheritdoc/>
        public override bool CanSeek => false;

        /// <inheritdoc/>
        public override bool CanWrite => false;

        /// <inheritdoc/>
        public override long Length => throw new NotSupportedException();

        /// <inheritdoc/>
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override void Flush()
        {
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        /// <inheritdoc/>
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
            return 0;
        }

        /// <inheritdoc/>
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        /// <inheritdoc/>
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
