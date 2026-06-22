using Kawa.Core;
using Kawa.Abstractions;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;
using System.Text.Json.Serialization.Metadata;

namespace Kawa.Web;

/// <summary>
/// Registers Kawa services with dependency injection.
/// </summary>
public static class KawaServiceCollectionExtensions
{
    /// <summary>
    /// Registers the core services required by Kawa endpoints.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <returns>The supplied service collection.</returns>
    public static IServiceCollection AddKawa(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<UseCaseExecutor>();
        services.TryAddSingleton<IResultMapper<IResult>, KawaHttpSuccessMapper>();
        services.TryAddSingleton<IErrorMapper<IResult>, KawaHttpErrorMapper>();
        services.TryAddSingleton<ITransportMapper<IResult>, KawaHttpTransportMapper>();

        return services;
    }

    /// <summary>
    /// Registers Kawa web conventions, including OpenAPI document generation.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <returns>The supplied service collection.</returns>
    public static IServiceCollection AddKawaWeb(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOpenApi(options =>
        {
            options.CreateSchemaReferenceId = CreateSchemaReferenceId;
            options.AddOperationTransformer(KawaOpenApiOperationTransformer.TransformAsync);
        });

        return services;
    }

    private static string? CreateSchemaReferenceId(JsonTypeInfo typeInfo)
    {
        var type = typeInfo.Type;

        // Kawa contracts commonly use nested Request and Response types. Their full names retain
        // the declaring contract and namespace, so generated clients cannot bind an endpoint to a
        // same-named schema from another use case. Dots also keep the resulting component IDs readable.
        if (type is { IsNested: true, FullName: not null })
        {
            return type.FullName.Replace('+', '.');
        }

        return OpenApiOptions.CreateDefaultSchemaReferenceId(typeInfo);
    }

    /// <summary>
    /// Registers every concrete use case found in the supplied assemblies.
    /// </summary>
    /// <param name="services">The service collection to update.</param>
    /// <param name="assemblies">The assemblies to scan for use cases.</param>
    /// <returns>The supplied service collection.</returns>
    public static IServiceCollection AddKawaUseCasesFromAssemblies(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assemblies);

        foreach (var assembly in assemblies)
        {
            ArgumentNullException.ThrowIfNull(assembly);

            foreach (var useCaseType in assembly.GetTypes().Where(IsConcreteClass))
            {
                if (KawaUseCaseContract.IsUseCaseType(useCaseType))
                {
                    var catalogEntry = KawaUseCaseCatalog.FromUseCaseType(useCaseType);
                    var contract = KawaUseCaseContract.FromUseCaseType(useCaseType);
                    services.AddSingleton(catalogEntry);
                    services.AddSingleton(contract.InterfaceType, useCaseType);
                }
            }
        }

        return services;
    }

    private static bool IsConcreteClass(Type type)
    {
        return type is { IsClass: true, IsAbstract: false } && !type.ContainsGenericParameters;
    }
}
