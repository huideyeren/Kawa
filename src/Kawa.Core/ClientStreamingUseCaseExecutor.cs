using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Kawa.Abstractions;

namespace Kawa.Core;

/// <summary>
/// Executes client-streaming use cases through Kawa's application flow boundary.
/// </summary>
public sealed class ClientStreamingUseCaseExecutor
{
    /// <summary>
    /// Executes a client-streaming use case with the supplied request sequence.
    /// </summary>
    public Task<KawaResult<TResponse>> ExecuteAsync<TRequest, TResponse>(
        IClientStreamingUseCase<TRequest, TResponse> useCase,
        IAsyncEnumerable<TRequest> requests,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(useCase);
        ArgumentNullException.ThrowIfNull(requests);
        return UseCaseTelemetry.RunAsync(() => useCase.ExecuteAsync(requests, cancellationToken),
            useCase.GetType(), "usecase.client_stream");
    }
}
