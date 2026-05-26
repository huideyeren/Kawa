using System.Reflection;

namespace Kawa.Abstractions;

/// <summary>
/// Builds a transport-independent catalog of Kawa use cases.
/// </summary>
public static class KawaUseCaseCatalog
{
    private static readonly IReadOnlyList<KawaErrorResponseMetadata> DefaultErrorResponses =
    [
        new(KawaErrorKind.Validation, "The request failed validation."),
        new(KawaErrorKind.Unauthorized, "The caller is not authenticated."),
        new(KawaErrorKind.Forbidden, "The caller is not allowed to perform the operation."),
        new(KawaErrorKind.NotFound, "The requested resource was not found."),
        new(KawaErrorKind.Conflict, "The operation conflicts with current application state."),
        new(KawaErrorKind.Unknown, "The failure has no more specific application category."),
    ];

    /// <summary>
    /// Creates a catalog entry for the supplied use case type.
    /// </summary>
    /// <param name="useCaseType">The use case implementation type.</param>
    /// <returns>The catalog entry for the use case.</returns>
    public static KawaUseCaseCatalogEntry FromUseCaseType(Type useCaseType)
    {
        ArgumentNullException.ThrowIfNull(useCaseType);

        var contract = GetUseCaseContract(useCaseType);
        var arguments = contract.GetGenericArguments();
        var attribute = useCaseType.GetCustomAttribute<KawaUseCaseAttribute>();

        var metadata = new KawaUseCaseMetadata(
            attribute?.Name ?? useCaseType.Name,
            attribute?.Summary,
            attribute?.Description,
            attribute?.Version ?? "v1",
            attribute?.Tags ?? []);

        return new KawaUseCaseCatalogEntry(
            useCaseType,
            arguments[0],
            arguments[1],
            metadata,
            GetErrorResponses(useCaseType));
    }

    /// <summary>
    /// Creates catalog entries for use cases found in the supplied assemblies.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <returns>The catalog entries found in the supplied assemblies.</returns>
    public static IReadOnlyList<KawaUseCaseCatalogEntry> FromAssemblies(params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        return assemblies
            .SelectMany(assembly =>
            {
                ArgumentNullException.ThrowIfNull(assembly);
                return assembly.GetTypes();
            })
            .Where(IsConcreteUseCaseType)
            .Select(FromUseCaseType)
            .ToArray();
    }

    private static IReadOnlyList<KawaErrorResponseMetadata> GetErrorResponses(Type useCaseType)
    {
        var declaredResponses = useCaseType.GetCustomAttributes<KawaErrorResponseAttribute>()
            .Select(attribute => new KawaErrorResponseMetadata(
                attribute.Kind,
                attribute.Description ?? GetDefaultErrorDescription(attribute.Kind)));

        return DefaultErrorResponses
            .Concat(declaredResponses)
            .GroupBy(response => response.Kind)
            .Select(group => group.Last())
            .ToArray();
    }

    private static Type GetUseCaseContract(Type useCaseType)
    {
        var contracts = useCaseType.GetInterfaces()
            .Where(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IUseCase<,>))
            .ToArray();

        return contracts.Length switch
        {
            1 => contracts[0],
            0 => throw new InvalidOperationException(
                $"{useCaseType.FullName} must implement IUseCase<TRequest, TResponse>."),
            _ => throw new InvalidOperationException(
                $"{useCaseType.FullName} must implement exactly one IUseCase<TRequest, TResponse> contract."),
        };
    }

    private static string GetDefaultErrorDescription(KawaErrorKind kind)
    {
        return DefaultErrorResponses.FirstOrDefault(response => response.Kind == kind)?.Description
            ?? "The use case failed.";
    }

    private static bool IsConcreteUseCaseType(Type type)
    {
        return type is { IsClass: true, IsAbstract: false }
            && !type.ContainsGenericParameters
            && type.GetInterfaces().Any(interfaceType =>
                interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IUseCase<,>));
    }
}
