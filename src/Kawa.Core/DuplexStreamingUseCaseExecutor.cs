using System;
using System.Collections.Generic;
using System.Threading;
using Kawa.Abstractions;

namespace Kawa.Core;

/// <summary>
/// Executes duplex-streaming use cases through Kawa's application flow boundary.
/// </summary>
public sealed class DuplexStreamingUseCaseExecutor
{
    /// <summary>
    /// Executes a duplex-streaming use case with the supplied request sequence.
    /// </summary>
    public IAsyncEnumerable<KawaResult<TResponse>> ExecuteAsync<TRequest, TResponse>(
        IDuplexStreamingUseCase<TRequest, TResponse> useCase,
        IAsyncEnumerable<TRequest> requests,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(useCase);
        ArgumentNullException.ThrowIfNull(requests);
        return UseCaseTelemetry.Stream(() => useCase.ExecuteAsync(requests, cancellationToken),
            useCase.GetType(), "usecase.duplex_stream", cancellationToken);
    }
}
