using System;
using System.Threading;
using System.Threading.Tasks;
using Kawa.Abstractions;

namespace Kawa.Core;

/// <summary>
/// Executes use cases through Kawa's application flow boundary.
/// </summary>
public sealed class UseCaseExecutor
{
    /// <summary>
    /// Executes a use case with the supplied request.
    /// </summary>
    /// <typeparam name="TRequest">The request contract type.</typeparam>
    /// <typeparam name="TResponse">The response contract type.</typeparam>
    /// <param name="useCase">The use case to execute.</param>
    /// <param name="request">The request contract to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result produced by the use case.</returns>
    public Task<KawaResult<TResponse>> ExecuteAsync<TRequest, TResponse>(IUseCase<TRequest, TResponse> useCase, TRequest request, CancellationToken cancellationToken = default)
    {
        if (useCase is null)
        {
            throw new ArgumentNullException(nameof(useCase));
        }

        return useCase.ExecuteAsync(request, cancellationToken);
    }
}
