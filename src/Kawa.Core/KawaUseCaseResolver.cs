namespace Kawa.Core;

/// <summary>Resolves an operation's exact implementation without confusing shared DTO contracts.</summary>
public static class KawaUseCaseResolver
{
    /// <summary>Resolves a concrete registration, or the matching implementation in contract registrations.</summary>
    /// <remarks>Resolution uses the supplied scope; it never creates a container or an implementation instance.</remarks>
    public static TContract Resolve<TImplementation, TContract>(IServiceProvider services)
        where TImplementation : class, TContract
    {
        ArgumentNullException.ThrowIfNull(services);
        if (services.GetService(typeof(TImplementation)) is TImplementation concrete) return concrete;
        if (services.GetService(typeof(IEnumerable<TContract>)) is IEnumerable<TContract> registrations)
        {
            var match = registrations.OfType<TImplementation>().LastOrDefault();
            if (match is not null) return match;
        }
        throw new InvalidOperationException($"Register {typeof(TImplementation).FullName} directly or as {typeof(TContract).FullName}.");
    }
}
