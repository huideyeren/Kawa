using Kawa.Core;
using Kawa.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection;

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
