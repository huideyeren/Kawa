namespace Kawa.Abstractions;

/// <summary>
/// Describes one use case in the public API catalog.
/// </summary>
/// <param name="Name">The stable use case name.</param>
/// <param name="Summary">The short use case summary.</param>
/// <param name="Description">The longer use case description.</param>
/// <param name="Version">The use case contract version.</param>
/// <param name="Tags">The catalog tags associated with the use case.</param>
/// <param name="Request">The request contract metadata.</param>
/// <param name="Response">The response contract metadata.</param>
/// <param name="ErrorResponses">The predictable error responses for the use case.</param>
public sealed record KawaApiCatalogUseCase(
    string Name,
    string? Summary,
    string? Description,
    string Version,
    IReadOnlyList<string> Tags,
    KawaApiCatalogContract Request,
    KawaApiCatalogContract Response,
    IReadOnlyList<KawaApiCatalogErrorResponse> ErrorResponses)
{
    /// <summary>
    /// Creates public API catalog use case metadata from a use case catalog entry.
    /// </summary>
    /// <param name="entry">The use case catalog entry.</param>
    /// <returns>The generated public API catalog use case metadata.</returns>
    public static KawaApiCatalogUseCase FromEntry(KawaUseCaseCatalogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return new KawaApiCatalogUseCase(
            entry.Metadata.Name,
            entry.Metadata.Summary,
            entry.Metadata.Description,
            entry.Metadata.Version,
            entry.Metadata.Tags,
            KawaApiCatalogContract.FromType(entry.RequestType),
            KawaApiCatalogContract.FromType(entry.ResponseType),
            entry.ErrorResponses
                .Select(KawaApiCatalogErrorResponse.FromMetadata)
                .ToArray());
    }
}
