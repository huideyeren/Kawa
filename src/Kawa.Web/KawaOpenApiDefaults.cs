namespace Kawa.Web;

/// <summary>
/// Provides the default Kawa OpenAPI document and UI routes.
/// </summary>
public static class KawaOpenApiDefaults
{
    /// <summary>
    /// Gets the default OpenAPI document route pattern.
    /// </summary>
    public const string DocumentPattern = "/openapi/{documentName}.json";

    /// <summary>
    /// Gets the default OpenAPI document name.
    /// </summary>
    public const string DocumentName = "v1";

    /// <summary>
    /// Gets the default OpenAPI document URL.
    /// </summary>
    public const string DocumentUrl = "/openapi/v1.json";

    /// <summary>
    /// Gets the default document display name used by OpenAPI UIs.
    /// </summary>
    public const string DisplayName = "Kawa v1";

    /// <summary>
    /// Gets the default Kawa API catalog route pattern.
    /// </summary>
    public const string ApiCatalogPattern = "/kawa/catalog.json";

    /// <summary>
    /// Gets the default Kawa API catalog endpoint name.
    /// </summary>
    public const string ApiCatalogEndpointName = "kawa.catalog";

    /// <summary>
    /// Gets the default Swagger UI route prefix.
    /// </summary>
    public const string SwaggerRoutePrefix = "swagger";

    /// <summary>
    /// Gets the default ReDoc route prefix.
    /// </summary>
    public const string ReDocRoutePrefix = "redoc";
}
