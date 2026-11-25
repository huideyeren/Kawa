using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Kawa.Aspire.Hosting;
using Xunit;

namespace Kawa.Aspire.Tests;

public sealed class KawaHostingExtensionsTests
{
    [Fact]
    public async Task MagicOnionProject_ExposesHttp2AndReference()
    {
        var builder = CreateBuilder();
        var service = builder.AddKawaMagicOnionProject("chat", ProjectPath("Kawa.Sample.CSharp"), targetPort: 5000);
        var consumer = builder.AddKawaCliProject("client", ProjectPath("Kawa.Sample.VB"), ["send", "hello world"])
            .WithKawaMagicOnionReference(service);

        var endpoint = Assert.Single(service.Resource.Annotations.OfType<EndpointAnnotation>(), item => item.Name == "grpc");
        Assert.Equal("http", endpoint.UriScheme);
        Assert.Equal("http2", endpoint.Transport);
        Assert.Equal(5000, endpoint.TargetPort);

        var environment = new Dictionary<string, object>();
        var context = new EnvironmentCallbackContext(builder.ExecutionContext, consumer.Resource, environment);
        foreach (var callback in consumer.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>())
            await callback.Callback(context);
        Assert.Contains(environment.Keys, key => key.StartsWith("services__chat__", StringComparison.Ordinal));
    }

    [Fact]
    public void DefaultGrpcPorts_AreAllocatedDynamicallyForMultipleProjects()
    {
        var builder = CreateBuilder();
        var first = builder.AddKawaMagicOnionProject("first", ProjectPath("Kawa.Sample.CSharp"));
        var second = builder.AddKawaMagicOnionProject("second", ProjectPath("Kawa.Sample.CSharp"));
        foreach (var resource in new[] { first, second })
        {
            var endpoint = Assert.Single(resource.Resource.Annotations.OfType<EndpointAnnotation>(), item => item.Name == "grpc");
            Assert.Null(endpoint.TargetPort);
        }
    }

    [Fact]
    public async Task CliProject_PreservesArgumentsAndExplicitDependencies()
    {
        var builder = CreateBuilder();
        var service = builder.AddKawaMagicOnionProject("chat", ProjectPath("Kawa.Sample.CSharp"));
        var cli = builder.AddKawaCliProject("client", ProjectPath("Kawa.Sample.VB"), ["send", "hello world"])
            .WithEnvironment("KAWA_MODE", "test")
            .WithKawaMagicOnionReference(service);
        var arguments = new List<object>();
        var context = new CommandLineArgsCallbackContext(arguments);
        foreach (var callback in cli.Resource.Annotations.OfType<CommandLineArgsCallbackAnnotation>())
            await callback.Callback(context);
        Assert.Equal(new object[] { "send", "hello world" }, arguments);
        Assert.Contains(cli.Resource.Annotations.OfType<WaitAnnotation>(), wait => wait.Resource == service.Resource);
    }

    private static string ProjectPath(string name)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Kawa.sln")))
            directory = directory.Parent;
        return Path.Combine(directory!.FullName, "samples", name, name + ".csproj");
    }

    private static IDistributedApplicationBuilder CreateBuilder() =>
        DistributedApplication.CreateBuilder(new DistributedApplicationOptions
        {
            DisableDashboard = true,
            Args = ["--hostBuilder:reloadConfigOnChange=false"],
        });
}
