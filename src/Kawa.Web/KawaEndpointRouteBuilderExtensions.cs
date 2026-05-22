using Kawa.Abstractions;
using Kawa.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Kawa.Web;

/// <summary>
/// Maps Kawa use cases to ASP.NET Core endpoints.
/// </summary>
public static class KawaEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps an HTTP POST endpoint that executes a Kawa use case resolved from dependency injection.
    /// </summary>
    /// <typeparam name="TRequest">The request body contract type.</typeparam>
    /// <typeparam name="TResponse">The successful response contract type.</typeparam>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern to map.</param>
    /// <returns>The mapped route handler builder.</returns>
    public static RouteHandlerBuilder MapKawaPost<TRequest, TResponse>(
        this IEndpointRouteBuilder endpoints,
        string pattern)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        return endpoints.MapPost(
            pattern,
            async (
                [FromBody] TRequest request,
                IServiceProvider services,
                CancellationToken cancellationToken) =>
            {
                var useCase = services.GetRequiredService<IUseCase<TRequest, TResponse>>();
                var executor = services.GetRequiredService<UseCaseExecutor>();
                var result = await executor.ExecuteAsync(useCase, request, cancellationToken);

                return KawaHttpResultConverter.ToIResult(result);
            });
    }
}
