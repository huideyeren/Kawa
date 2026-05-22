using Kawa.Core;
using Kawa.Web;
using Microsoft.Extensions.DependencyInjection;

namespace Kawa.Web.Tests;

/// <summary>
/// Verifies Kawa dependency injection registration.
/// </summary>
public sealed class KawaServiceCollectionExtensionsTests
{
    /// <summary>
    /// Verifies that AddKawa registers the core services needed by Kawa endpoints.
    /// </summary>
    [Fact]
    public void AddKawa_RegistersUseCaseExecutor()
    {
        var services = new ServiceCollection();

        services.AddKawa();

        using var serviceProvider = services.BuildServiceProvider();
        Assert.NotNull(serviceProvider.GetService<UseCaseExecutor>());
    }
}
