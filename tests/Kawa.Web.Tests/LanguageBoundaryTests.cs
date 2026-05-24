using System.Net;
using System.Net.Http.Json;
using Kawa.Abstractions;
using Kawa.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FSharpCreateUserRequest = Kawa.Sample.Mixed.UseCases.CreateUserRequest;
using FSharpCreateUserResponse = Kawa.Sample.Mixed.UseCases.CreateUserResponse;
using FSharpCreateUserUseCase = Kawa.Sample.Mixed.UseCases.CreateUserUseCase;
using VbCreateUserRequest = Kawa.Sample.VB.UseCases.CreateUserRequest;
using VbCreateUserResponse = Kawa.Sample.VB.UseCases.CreateUserResponse;
using VbCreateUserUseCase = Kawa.Sample.VB.UseCases.CreateUserUseCase;

namespace Kawa.Web.Tests;

/// <summary>
/// Verifies that non-C# use case implementations keep the same C# friendly HTTP boundary.
/// </summary>
public sealed class LanguageBoundaryTests
{
    /// <summary>
    /// Verifies that an F# use case is executed through the same HTTP endpoint contract.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task Post_ExecutesFSharpUseCaseThroughCSharpFriendlyBoundary()
    {
        using var host = await StartHostAsync<FSharpCreateUserRequest, FSharpCreateUserResponse>(
            new FSharpCreateUserUseCase());
        using var client = host.GetTestClient();

        var httpResponse = await client.PostAsJsonAsync("/users", new FSharpCreateUserRequest { Name = "Ada" });
        var response = await httpResponse.Content.ReadFromJsonAsync<FSharpCreateUserResponse>();

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        Assert.NotNull(response);
        Assert.False(string.IsNullOrWhiteSpace(response.Id));
        Assert.Equal("Ada", response.Name);
    }

    /// <summary>
    /// Verifies that a VB.NET use case is executed through the same HTTP endpoint contract.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task Post_ExecutesVbUseCaseThroughCSharpFriendlyBoundary()
    {
        using var host = await StartHostAsync<VbCreateUserRequest, VbCreateUserResponse>(
            new VbCreateUserUseCase());
        using var client = host.GetTestClient();

        var httpResponse = await client.PostAsJsonAsync("/users", new VbCreateUserRequest { Name = "Ada" });
        var response = await httpResponse.Content.ReadFromJsonAsync<VbCreateUserResponse>();

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        Assert.NotNull(response);
        Assert.False(string.IsNullOrWhiteSpace(response.Id));
        Assert.Equal("Ada", response.Name);
    }

    private static async Task<IHost> StartHostAsync<TRequest, TResponse>(
        IUseCase<TRequest, TResponse> useCase)
    {
        return await new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddKawa();
                    services.AddSingleton(useCase);
                });
                webHost.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                        endpoints.MapKawaPost<TRequest, TResponse>("/users"));
                });
            })
            .StartAsync();
    }
}
