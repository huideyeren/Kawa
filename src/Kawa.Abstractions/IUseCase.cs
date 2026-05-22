using System.Threading;
using System.Threading.Tasks;

namespace Kawa.Abstractions;

/// <summary>
/// Defines an application use case with a request and response contract.
/// </summary>
/// <typeparam name="TRequest">The request contract accepted by the use case.</typeparam>
/// <typeparam name="TResponse">The response contract returned by the use case.</typeparam>
public interface IUseCase<TRequest, TResponse>
{
    /// <summary>
    /// Executes the use case for the supplied request.
    /// </summary>
    /// <param name="request">The request contract to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A result containing either the response or an application error.</returns>
    Task<KawaResult<TResponse>> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default);
}
