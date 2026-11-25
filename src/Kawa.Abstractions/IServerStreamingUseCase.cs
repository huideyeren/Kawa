using System.Collections.Generic;
using System.Threading;

namespace Kawa.Abstractions;

/// <summary>
/// Defines an application use case that sends a sequence of responses for one request.
/// </summary>
/// <typeparam name="TRequest">The request contract accepted by the use case.</typeparam>
/// <typeparam name="TResponse">The response contract produced by the use case.</typeparam>
public interface IServerStreamingUseCase<TRequest, TResponse>
{
    /// <summary>
    /// Produces responses for the supplied request.
    /// </summary>
    /// <param name="request">The request contract to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A sequence of successful responses or a terminal application error.</returns>
    IAsyncEnumerable<KawaResult<TResponse>> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default);
}
