namespace Kawa.Abstractions;

/// <summary>
/// Describes a predictable error response in the public API catalog.
/// </summary>
/// <param name="Kind">The Kawa error kind name.</param>
/// <param name="Description">The error description.</param>
public sealed record KawaApiCatalogErrorResponse(string Kind, string Description)
{
    /// <summary>
    /// Creates public API catalog error response metadata.
    /// </summary>
    /// <param name="metadata">The transport-independent error response metadata.</param>
    /// <returns>The generated public API catalog error response metadata.</returns>
    public static KawaApiCatalogErrorResponse FromMetadata(KawaErrorResponseMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        return new KawaApiCatalogErrorResponse(metadata.Kind.ToString(), metadata.Description);
    }
}
