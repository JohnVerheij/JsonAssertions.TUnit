using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace JsonAssertions;

/// <summary>Reads a response body the same way for every assertion and extraction method that takes an
/// <see cref="HttpResponseMessage"/>.</summary>
internal static class ResponseBody
{
    /// <summary>The failure for an empty body: a 204, a response built without content, or a body the test already
    /// read through its stream.</summary>
    internal const string Empty =
        "the response body is empty; a body read through its stream before the assertion is gone, unless the content "
        + "was buffered first with LoadIntoBufferAsync";

    /// <summary>The message of a body read that the assertion's token canceled.</summary>
    internal const string AssertionCanceled = "The response body read was canceled before the JSON assertion ran.";

    /// <summary>The message of a body read that the extraction's token canceled.</summary>
    internal const string ExtractionCanceled = "The response body read was canceled before the JSON value was read.";

    /// <summary>Reads the body as text. A token that is already canceled cancels the call, also for a body in memory,
    /// for which <see cref="HttpContent"/> ignores the token; a read that the token cancels throws an
    /// <see cref="OperationCanceledException"/> with <paramref name="canceledMessage"/> that carries the token.</summary>
    /// <param name="response">The response.</param>
    /// <param name="canceledMessage">The message of a canceled read.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The body, decoded from its charset or byte order mark; empty when there is none.</returns>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> is canceled.</exception>
    internal static async Task<string> ReadAsync(
        HttpResponseMessage response, string canceledMessage, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(canceledMessage, exception, cancellationToken);
        }
    }
}
