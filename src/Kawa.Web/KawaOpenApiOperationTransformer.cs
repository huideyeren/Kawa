using Kawa.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Kawa.Web;

internal static class KawaOpenApiOperationTransformer
{
    public static Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(context);

        var catalogEntry = GetCatalogEntry(context.Description);
        if (catalogEntry is null)
        {
            return Task.CompletedTask;
        }

        foreach (var errorResponse in catalogEntry.ErrorResponses)
        {
            var statusCode = GetStatusCode(errorResponse.Kind).ToString();
            if (operation.Responses?.TryGetValue(statusCode, out var response) == true)
            {
                response.Description = errorResponse.Description;
            }
        }

        return Task.CompletedTask;
    }

    private static KawaUseCaseCatalogEntry? GetCatalogEntry(ApiDescription description)
    {
        return description.ActionDescriptor.EndpointMetadata
            .OfType<KawaUseCaseCatalogEntry>()
            .SingleOrDefault();
    }

    private static int GetStatusCode(KawaErrorKind kind)
    {
        return kind switch
        {
            KawaErrorKind.Validation => StatusCodes.Status400BadRequest,
            KawaErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
            KawaErrorKind.Forbidden => StatusCodes.Status403Forbidden,
            KawaErrorKind.NotFound => StatusCodes.Status404NotFound,
            KawaErrorKind.Conflict => StatusCodes.Status409Conflict,
            KawaErrorKind.Unknown => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError,
        };
    }
}
