using Kawa.Abstractions;

namespace Kawa.Web;

internal sealed record KawaUseCaseContract(Type InterfaceType, Type RequestType, Type ResponseType)
{
    public static bool IsUseCaseType(Type useCaseType)
    {
        ArgumentNullException.ThrowIfNull(useCaseType);

        return useCaseType.GetInterfaces()
            .Any(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IUseCase<,>));
    }

    public static KawaUseCaseContract FromUseCaseType(Type useCaseType)
    {
        ArgumentNullException.ThrowIfNull(useCaseType);

        var catalogEntry = KawaUseCaseCatalog.FromUseCaseType(useCaseType);
        return new KawaUseCaseContract(
            typeof(IUseCase<,>).MakeGenericType(catalogEntry.RequestType, catalogEntry.ResponseType),
            catalogEntry.RequestType,
            catalogEntry.ResponseType);
    }
}
