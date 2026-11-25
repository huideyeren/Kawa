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
            .GroupBy(entry => (entry.UseCaseType, entry.RequestType, entry.ResponseType, entry.Metadata.Name, entry.Metadata.Version))
            .Select(group =>
            {
                var first = group.First();
                var transports = group.SelectMany(entry => entry.Transports ?? [])
                    .Distinct().OrderBy(transport => transport.Kind, StringComparer.Ordinal).ToArray();
                return KawaApiCatalogUseCase.FromEntry(first with
                {
                    Transports = transports.Length == 0 ? null : transports,
                });
            })
            .OrderBy(useCase => useCase.Name, StringComparer.Ordinal)
            .ToArray();

        return new KawaApiCatalog(useCases);
    }
}
