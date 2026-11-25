using Kawa.Abstractions;

namespace Kawa.Core.Tests;

/// <summary>Protects implementation identity and ordinary last-registration overrides.</summary>
public sealed class KawaUseCaseResolverTests
{
    /// <summary>Repeated registrations of one implementation keep DI's last-registration convention.</summary>
    [Fact]
    public void Resolve_SelectsLastMatchingImplementation()
    {
        var first = new Send();
        var last = new Send();
        var provider = new Provider(null, new IUseCase<string, string>[] { first, last, new Other() });
        Assert.Same(last, Kawa.Core.KawaUseCaseResolver.Resolve<Send, IUseCase<string, string>>(provider));
    }

    /// <summary>An explicit concrete registration takes priority over generic contract bindings.</summary>
    [Fact]
    public void Resolve_PrefersConcreteRegistration()
    {
        var concrete = new Send();
        Assert.Same(concrete, Kawa.Core.KawaUseCaseResolver.Resolve<Send, IUseCase<string,string>>(new Provider(concrete, [])));
    }

    /// <summary>A different implementation with the same DTOs must never be substituted.</summary>
    [Fact]
    public void Resolve_RejectsMissingImplementation()
    {
        Assert.Throws<InvalidOperationException>(() => Kawa.Core.KawaUseCaseResolver.Resolve<Send, IUseCase<string,string>>(new Provider(null, [new Other()])));
    }

    private sealed class Provider(Send? concrete, IUseCase<string,string>[] registrations) : IServiceProvider
    {
        public object? GetService(Type serviceType) => serviceType == typeof(Send) ? concrete : registrations;
    }
    private class Send : IUseCase<string,string>
    {
        public Task<KawaResult<string>> ExecuteAsync(string request, CancellationToken cancellationToken = default) => Task.FromResult(KawaResult<string>.Success(request));
    }
    private sealed class Other : IUseCase<string,string>
    {
        public Task<KawaResult<string>> ExecuteAsync(string request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
