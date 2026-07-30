# Kawa Use Case Patterns

## Compact C# Use Case

Use this shape when the application keeps contracts nested with the use case:

```csharp
using Kawa.Abstractions;

[KawaUseCase(
    "users.create",
    Summary = "Create user",
    Description = "Creates a user account.",
    Version = "v1",
    Tags = new[] { "Users" })]
[KawaErrorResponse(KawaErrorKind.Validation, Description = "The supplied user fields are invalid.")]
public sealed class CreateUser
    : IUseCase<CreateUser.Request, CreateUser.Response>
{
    public sealed record Request(string Name, string Email);

    public sealed record Response(Guid UserId, string Name, string Email);

    public Task<KawaResult<Response>> ExecuteAsync(
        Request request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            var error = new KawaError(KawaErrorKind.Validation, "Email is required.");
            return Task.FromResult(KawaResult<Response>.Failure(error));
        }

        var response = new Response(Guid.NewGuid(), request.Name, request.Email);
        return Task.FromResult(KawaResult<Response>.Success(response));
    }
}
```

## Split Contract And Use Case

Use this shape when the application has `Contracts/` and `UseCases/` folders:

```csharp
namespace MyApp.Contracts.Users;

public static class CreateUser
{
    public sealed record Request(string Name, string Email);

    public sealed record Response(Guid UserId, string Name, string Email);
}
```

```csharp
using Kawa.Abstractions;
using MyApp.Contracts.Users;

namespace MyApp.UseCases.Users;

[KawaUseCase("users.create", Summary = "Create user", Version = "v1", Tags = new[] { "Users" })]
[KawaErrorResponse(KawaErrorKind.Validation, Description = "The supplied user fields are invalid.")]
public sealed class CreateUserUseCase
    : IUseCase<CreateUser.Request, CreateUser.Response>
{
    public Task<KawaResult<CreateUser.Response>> ExecuteAsync(
        CreateUser.Request request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            var error = new KawaError(KawaErrorKind.Validation, "Email is required.");
            return Task.FromResult(KawaResult<CreateUser.Response>.Failure(error));
        }

        var response = new CreateUser.Response(Guid.NewGuid(), request.Name, request.Email);
        return Task.FromResult(KawaResult<CreateUser.Response>.Success(response));
    }
}
```

## Web Mapping

Register Kawa and map endpoints through the app's existing host:

```csharp
using Kawa.Web;

builder.Services
    .AddKawa()
    .AddKawaUseCasesFromAssemblies(typeof(CreateUserUseCase).Assembly)
    .AddKawaWeb();

app.MapKawaPost<CreateUserUseCase>("/users");
app.MapKawaApiCatalog();
app.MapKawaOpenApi();
```

For explicit cross-language contracts:

```csharp
builder.Services.AddSingleton<IUseCase<CreateUserRequest, CreateUserResponse>, CreateUserUseCase>();

app.MapKawaPost<CreateUserRequest, CreateUserResponse>("/users");
```

## Failure Modeling

Use `KawaErrorKind` for predictable application failures:

```csharp
return Task.FromResult(KawaResult<Response>.Failure(
    new KawaError(KawaErrorKind.NotFound, "User was not found.")));
```

Do not return HTTP status codes, `Results.BadRequest`, or `IResult` from a use case. HTTP mapping belongs to `Kawa.Web`.

## XML Documentation

When generated clients need schema descriptions, enable XML docs in the project that defines public contracts:

```xml
<GenerateDocumentationFile>true</GenerateDocumentationFile>
```

Add concise XML summaries to public contract types and properties.

