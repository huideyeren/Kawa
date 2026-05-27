using Microsoft.AspNetCore.Builder;

namespace Kawa.Web;

/// <summary>
/// Maps Kawa OpenAPI UI middleware.
/// </summary>
public static class KawaOpenApiUiApplicationBuilderExtensions
{
    /// <summary>
    /// Maps Swagger UI for the Kawa OpenAPI document.
    /// </summary>
    /// <remarks>
    /// Map this middleware only where API documentation UI should be public. The recommended convention is
    /// to call it from an <c>IsDevelopment()</c> block unless production UI exposure is intentional.
    /// </remarks>
    /// <param name="app">The application builder.</param>
    /// <param name="routePrefix">The route prefix for Swagger UI.</param>
    /// <param name="documentUrl">The OpenAPI document URL.</param>
    /// <param name="documentName">The display name for the OpenAPI document.</param>
    /// <returns>The supplied application builder.</returns>
    public static IApplicationBuilder MapKawaSwagger(
        this IApplicationBuilder app,
        string routePrefix = KawaOpenApiDefaults.SwaggerRoutePrefix,
        string documentUrl = KawaOpenApiDefaults.DocumentUrl,
        string documentName = KawaOpenApiDefaults.DisplayName)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentException.ThrowIfNullOrWhiteSpace(routePrefix);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentName);

        app.UseSwaggerUI(options =>
        {
            options.RoutePrefix = routePrefix;
            options.DocumentTitle = documentName;
            options.SwaggerEndpoint(documentUrl, documentName);
        });

        return app;
    }

    /// <summary>
    /// Maps ReDoc for the Kawa OpenAPI document.
    /// </summary>
    /// <remarks>
    /// Map this middleware only where API documentation UI should be public. The recommended convention is
    /// to call it from an <c>IsDevelopment()</c> block unless production UI exposure is intentional.
    /// </remarks>
    /// <param name="app">The application builder.</param>
    /// <param name="routePrefix">The route prefix for ReDoc.</param>
    /// <param name="documentUrl">The OpenAPI document URL.</param>
    /// <param name="documentName">The display name for the OpenAPI document.</param>
    /// <returns>The supplied application builder.</returns>
    public static IApplicationBuilder MapKawaReDoc(
        this IApplicationBuilder app,
        string routePrefix = KawaOpenApiDefaults.ReDocRoutePrefix,
        string documentUrl = KawaOpenApiDefaults.DocumentUrl,
        string documentName = KawaOpenApiDefaults.DisplayName)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentException.ThrowIfNullOrWhiteSpace(routePrefix);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentName);

        app.UseReDoc(options =>
        {
            options.RoutePrefix = routePrefix;
            options.DocumentTitle = documentName;
            options.SpecUrl(documentUrl);
        });

        return app;
    }
}
