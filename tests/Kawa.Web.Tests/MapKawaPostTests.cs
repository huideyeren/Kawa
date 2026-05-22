using System.Net;
using System.Net.Http.Json;
using Kawa.Abstractions;
using Kawa.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Kawa.Web.Tests;

/// <summary>
/// Verifies the HTTP contract exposed by <see cref="KawaEndpointRouteBuilderExtensions.MapKawaPost{TRequest,TResponse}" />.
/// </summary>
public sealed class MapKawaPostTests
{
    /// <summary>
    /// Verifies that a POST body is bound to the use case request and the successful value is returned.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task Post_BindsRequestExecutesUseCaseAndReturnsSuccessfulResponse()
    {
        var useCase = new RecordingCreateUserUseCase();
        using var host = await StartHostAsync(useCase);
        using var client = host.GetTestClient();

        var httpResponse = await client.PostAsJsonAsync("/users", new CreateUserRequest("Ada"));

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        Assert.Equal(new CreateUserRequest("Ada"), useCase.LastRequest);
        Assert.Equal(
            new CreateUserResponse("Created Ada"),
            await httpResponse.Content.ReadFromJsonAsync<CreateUserResponse>());
    }

    /// <summary>
    /// Verifies that a use case failure is converted at the HTTP boundary.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task Post_ConvertsUseCaseFailureToHttpStatusCode()
    {
        using var host = await StartHostAsync(new InvalidCreateUserUseCase());
        using var client = host.GetTestClient();

        var httpResponse = await client.PostAsJsonAsync("/users", new CreateUserRequest(""));

        Assert.Equal(HttpStatusCode.BadRequest, httpResponse.StatusCode);
    }

    private static async Task<IHost> StartHostAsync(IUseCase<CreateUserRequest, CreateUserResponse> useCase)
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
                        endpoints.MapKawaPost<CreateUserRequest, CreateUserResponse>("/users"));
                });
            })
            .StartAsync();
    }

    private sealed record CreateUserRequest(string Name);

    private sealed record CreateUserResponse(string Message);

    private sealed class RecordingCreateUserUseCase : IUseCase<CreateUserRequest, CreateUserResponse>
    {
        public CreateUserRequest? LastRequest { get; private set; }

        public Task<KawaResult<CreateUserResponse>> ExecuteAsync(
            CreateUserRequest request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(KawaResult<CreateUserResponse>.Success(new CreateUserResponse($"Created {request.Name}")));
        }
    }

    private sealed class InvalidCreateUserUseCase : IUseCase<CreateUserRequest, CreateUserResponse>
    {
        public Task<KawaResult<CreateUserResponse>> ExecuteAsync(
            CreateUserRequest request,
            CancellationToken cancellationToken = default)
        {
            var error = new KawaError(KawaErrorKind.Validation, "Name is required.");
            return Task.FromResult(KawaResult<CreateUserResponse>.Failure(error));
        }
    }
}
