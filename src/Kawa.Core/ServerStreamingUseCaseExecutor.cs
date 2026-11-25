using System;
using System.Collections.Generic;
using System.Threading;
using Kawa.Abstractions;

namespace Kawa.Core;

/// <summary>
/// Executes server-streaming use cases through Kawa's application flow boundary.
/// </summary>
public sealed class ServerStreamingUseCaseExecutor
{
    /// <summary>
    /// Executes a server-streaming use case with the supplied request.
    /// </summary>
    public IAsyncEnumerable<KawaResult<TResponse>> ExecuteAsync<TRequest, TResponse>(
        IServerStreamingUseCase<TRequest, TResponse> useCase,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(useCase);
        return UseCaseTelemetry.Stream(() => useCase.ExecuteAsync(request, cancellationToken),
            useCase.GetType(), "usecase.server_stream", cancellationToken);
    }
}
