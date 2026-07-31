using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kawa.Abstractions;

/// <summary>
/// Defines an application use case that receives a sequence of requests and returns one response.
/// </summary>
/// <typeparam name="TRequest">The request contract accepted by the use case.</typeparam>
/// <typeparam name="TResponse">The response contract produced by the use case.</typeparam>
public interface IClientStreamingUseCase<TRequest, TResponse>
{
    /// <summary>
    /// Processes the supplied request sequence.
    /// </summary>
    /// <param name="requests">The request contracts to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A result containing either the response or an application error.</returns>
    Task<KawaResult<TResponse>> ExecuteAsync(IAsyncEnumerable<TRequest> requests, CancellationToken cancellationToken = default);
}
