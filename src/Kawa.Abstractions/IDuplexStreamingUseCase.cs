using System.Collections.Generic;
using System.Threading;

namespace Kawa.Abstractions;

/// <summary>
/// Defines an application use case that receives and produces sequences concurrently.
/// </summary>
/// <typeparam name="TRequest">The request contract accepted by the use case.</typeparam>
/// <typeparam name="TResponse">The response contract produced by the use case.</typeparam>
public interface IDuplexStreamingUseCase<TRequest, TResponse>
{
    /// <summary>
    /// Produces responses while processing the supplied request sequence.
    /// </summary>
    /// <param name="requests">The request contracts to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A sequence of successful responses or a terminal application error.</returns>
    IAsyncEnumerable<KawaResult<TResponse>> ExecuteAsync(IAsyncEnumerable<TRequest> requests, CancellationToken cancellationToken = default);
}
