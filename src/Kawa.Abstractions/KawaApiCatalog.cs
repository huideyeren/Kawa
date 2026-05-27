namespace Kawa.Abstractions;

/// <summary>
/// Describes the public API catalog generated from Kawa use case contracts.
/// </summary>
/// <param name="UseCases">The use cases exposed by the catalog.</param>
public sealed record KawaApiCatalog(IReadOnlyList<KawaApiCatalogUseCase> UseCases)
{
    /// <summary>
    /// Creates an API catalog from use case catalog entries.
    /// </summary>
    /// <param name="entries">The use case catalog entries.</param>
    /// <returns>The generated API catalog.</returns>
    public static KawaApiCatalog FromEntries(IEnumerable<KawaUseCaseCatalogEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var useCases = entries
            .Select(KawaApiCatalogUseCase.FromEntry)
            .OrderBy(useCase => useCase.Name, StringComparer.Ordinal)
            .ToArray();

        return new KawaApiCatalog(useCases);
    }
}
