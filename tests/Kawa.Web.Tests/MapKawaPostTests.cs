using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Kawa.Abstractions;
using Kawa.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
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

    /// <summary>
    /// Verifies that a POST endpoint can be mapped from the use case type alone.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task Post_CanInferRequestAndResponseFromUseCaseType()
    {
        using var host = await StartConventionalHostAsync();
        using var client = host.GetTestClient();

        var httpResponse = await client.PostAsJsonAsync("/users", new ConventionalCreateUser.Request("Ada"));

        Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        Assert.Equal(
            new ConventionalCreateUser.Response("Created Ada"),
            await httpResponse.Content.ReadFromJsonAsync<ConventionalCreateUser.Response>());
    }

    /// <summary>
    /// Verifies that POST mappings expose request, response, and error metadata for OpenAPI.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task Post_AddsContractFirstOpenApiMetadata()
    {
        using var host = await StartConventionalHostAsync();

        var endpoint = host.Services.GetRequiredService<EndpointDataSource>().Endpoints.Single();
        var acceptsMetadata = endpoint.Metadata.GetRequiredMetadata<IAcceptsMetadata>();
        var producesMetadata = endpoint.Metadata.GetOrderedMetadata<IProducesResponseTypeMetadata>();

        Assert.Equal(typeof(ConventionalCreateUser.Request), acceptsMetadata.RequestType);
        Assert.Contains(
            producesMetadata,
            metadata => metadata.StatusCode == StatusCodes.Status200OK
                && metadata.Type == typeof(ConventionalCreateUser.Response));
        Assert.Contains(
            producesMetadata,
            metadata => metadata.StatusCode == StatusCodes.Status400BadRequest
                && metadata.Type == typeof(KawaError));
        Assert.Contains(
            producesMetadata,
            metadata => metadata.StatusCode == StatusCodes.Status404NotFound
                && metadata.Type == typeof(KawaError));
        Assert.Contains(
            producesMetadata,
            metadata => metadata.StatusCode == StatusCodes.Status409Conflict
                && metadata.Type == typeof(KawaError));
        Assert.Contains(
            producesMetadata,
            metadata => metadata.StatusCode == StatusCodes.Status500InternalServerError
                && metadata.Type == typeof(ProblemDetails));
    }

    /// <summary>
    /// Verifies that use case catalog metadata is added to the mapped endpoint.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task Post_AddsUseCaseCatalogMetadata()
    {
        using var host = await StartConventionalHostAsync();

        var endpoint = host.Services.GetRequiredService<EndpointDataSource>().Endpoints.Single();
        var catalogEntry = endpoint.Metadata.GetRequiredMetadata<KawaUseCaseCatalogEntry>();
        var nameMetadata = endpoint.Metadata.GetRequiredMetadata<IEndpointNameMetadata>();
        var summaryMetadata = endpoint.Metadata.GetRequiredMetadata<IEndpointSummaryMetadata>();
        var descriptionMetadata = endpoint.Metadata.GetRequiredMetadata<IEndpointDescriptionMetadata>();
        var tagsMetadata = endpoint.Metadata.GetRequiredMetadata<ITagsMetadata>();

        Assert.Equal("users.create", catalogEntry.Metadata.Name);
        Assert.Equal("v1", catalogEntry.Metadata.Version);
        Assert.Equal(typeof(ConventionalCreateUser.Request), catalogEntry.RequestType);
        Assert.Equal(typeof(ConventionalCreateUser.Response), catalogEntry.ResponseType);
        Assert.Contains(catalogEntry.ErrorResponses, response => response.Kind == KawaErrorKind.Validation);
        Assert.Equal("users.create", nameMetadata.EndpointName);
        Assert.Equal("Create user", summaryMetadata.Summary);
        Assert.Equal("Creates a user account.", descriptionMetadata.Description);
        Assert.Contains("Users", tagsMetadata.Tags);
    }

    /// <summary>
    /// Verifies that Kawa web conventions expose an OpenAPI document with use case metadata.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task OpenApi_ExposesUseCaseContractAndErrorMetadata()
    {
        using var host = await StartOpenApiHostAsync();
        using var client = host.GetTestClient();

        using var document = await JsonDocument.ParseAsync(
            await client.GetStreamAsync(KawaOpenApiDefaults.DocumentUrl));

        var operation = document.RootElement
            .GetProperty("paths")
            .GetProperty("/users")
            .GetProperty("post");
        var responses = operation.GetProperty("responses");

        Assert.Equal("users.create", operation.GetProperty("operationId").GetString());
        Assert.Equal("Create user", operation.GetProperty("summary").GetString());
        Assert.Contains(
            operation.GetProperty("tags").EnumerateArray(),
            tag => tag.GetString() == "Users");
        Assert.True(operation.GetProperty("requestBody").TryGetProperty("content", out _));
        Assert.True(responses.GetProperty("200").TryGetProperty("content", out _));
        Assert.Equal(
            "The supplied user fields are invalid.",
            responses.GetProperty("400").GetProperty("description").GetString());
        Assert.Equal(
            "The requested resource was not found.",
            responses.GetProperty("404").GetProperty("description").GetString());
        Assert.Equal(
            "The failure has no more specific application category.",
            responses.GetProperty("500").GetProperty("description").GetString());
        Assert.True(
            responses.GetProperty("500")
                .GetProperty("content")
                .TryGetProperty("application/problem+json", out _));
    }

    /// <summary>
    /// Verifies that Kawa web conventions expose a contract-first API catalog.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task ApiCatalog_ExposesMappedUseCaseContracts()
    {
        using var host = await StartOpenApiHostAsync();
        using var client = host.GetTestClient();

        using var document = await JsonDocument.ParseAsync(
            await client.GetStreamAsync(KawaOpenApiDefaults.ApiCatalogPattern));

        var useCase = Assert.Single(document.RootElement.GetProperty("useCases").EnumerateArray());
        var request = useCase.GetProperty("request");
        var response = useCase.GetProperty("response");
        var errors = useCase.GetProperty("errorResponses");

        Assert.Equal("users.create", useCase.GetProperty("name").GetString());
        Assert.Equal("Create user", useCase.GetProperty("summary").GetString());
        Assert.Equal("Creates a user account.", useCase.GetProperty("description").GetString());
        Assert.Equal("v1", useCase.GetProperty("version").GetString());
        Assert.Contains(
            useCase.GetProperty("tags").EnumerateArray(),
            tag => tag.GetString() == "Users");
        Assert.Equal(nameof(ConventionalCreateUser.Request), request.GetProperty("name").GetString());
        Assert.Equal(typeof(ConventionalCreateUser.Request).FullName, request.GetProperty("fullName").GetString());
        Assert.Equal(nameof(ConventionalCreateUser.Response), response.GetProperty("name").GetString());
        Assert.Equal(typeof(ConventionalCreateUser.Response).FullName, response.GetProperty("fullName").GetString());
        Assert.Contains(
            errors.EnumerateArray(),
            error => error.GetProperty("kind").GetString() == nameof(KawaErrorKind.Validation)
                && error.GetProperty("description").GetString() == "The supplied user fields are invalid.");
    }

    /// <summary>
    /// Verifies that Kawa web conventions expose Swagger UI and ReDoc.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task OpenApi_ExposesSwaggerAndReDoc()
    {
        using var host = await StartOpenApiHostAsync();
        using var client = host.GetTestClient();

        var swaggerHtml = await client.GetStringAsync($"/{KawaOpenApiDefaults.SwaggerRoutePrefix}/index.html");
        var redocHtml = await client.GetStringAsync($"/{KawaOpenApiDefaults.ReDocRoutePrefix}/index.html");

        Assert.Contains("swagger-ui", swaggerHtml);
        Assert.Contains("redoc", redocHtml);
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

    private static async Task<IHost> StartConventionalHostAsync()
    {
        return await new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddKawa();
                    services.AddSingleton<
                        IUseCase<ConventionalCreateUser.Request, ConventionalCreateUser.Response>,
                        ConventionalCreateUser>();
                });
                webHost.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                        endpoints.MapKawaPost<ConventionalCreateUser>("/users"));
                });
            })
            .StartAsync();
    }

    private static async Task<IHost> StartOpenApiHostAsync()
    {
        return await new HostBuilder()
            .ConfigureWebHost(webHost =>
            {
                webHost.UseTestServer();
                webHost.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddKawa();
                    services.AddKawaWeb();
                    services.AddSingleton<
                        IUseCase<ConventionalCreateUser.Request, ConventionalCreateUser.Response>,
                        ConventionalCreateUser>();
                });
                webHost.Configure(app =>
                {
                    app.UseRouting();
                    app.MapKawaSwagger();
                    app.MapKawaReDoc();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapKawaPost<ConventionalCreateUser>("/users");
                        endpoints.MapKawaApiCatalog();
                        endpoints.MapKawaOpenApi();
                    });
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

    [KawaUseCase(
        "users.create",
        Summary = "Create user",
        Description = "Creates a user account.",
        Version = "v1",
        Tags = new[] { "Users" })]
    [KawaErrorResponse(KawaErrorKind.Validation, Description = "The supplied user fields are invalid.")]
    private sealed class ConventionalCreateUser
        : IUseCase<ConventionalCreateUser.Request, ConventionalCreateUser.Response>
    {
        public sealed record Request(string Name);

        public sealed record Response(string Message);

        public Task<KawaResult<Response>> ExecuteAsync(
            Request request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(KawaResult<Response>.Success(new Response($"Created {request.Name}")));
        }
    }
}
