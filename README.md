## Design Principles

Kawa is a contract-first .NET web framework that lays a thin waterway on top of ASP.NET Core.

Kawa keeps application flow centered on request/response contracts and use cases. ASP.NET Core stays as the host and transport layer; Kawa only provides the small boundary pieces needed to execute use cases and translate `KawaResult<T>` values into HTTP responses.

## Packages

- `Kawa.Abstractions`: shared use case, result, and error contracts
- `Kawa.Core`: transport-independent use case execution
- `Kawa.Web`: ASP.NET Core Minimal API integration
- `Kawa.FSharp`: F# helpers

## Installation

```bash
dotnet add package Kawa.Web
```

`Kawa.Web` brings in the core Kawa runtime pieces needed for ASP.NET Core Minimal API integration.

## Quick Start

Define request and response contracts:

```csharp
public sealed record CreateUserRequest(string Name);

public sealed record CreateUserResponse(string Message);
```

Implement a use case:

```csharp
using Kawa.Abstractions;

public sealed class CreateUserUseCase
    : IUseCase<CreateUserRequest, CreateUserResponse>
{
    public Task<KawaResult<CreateUserResponse>> ExecuteAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            var error = new KawaError(KawaErrorKind.Validation, "Name is required.");
            return Task.FromResult(KawaResult<CreateUserResponse>.Failure(error));
        }

        var response = new CreateUserResponse($"Created user {request.Name}.");
        return Task.FromResult(KawaResult<CreateUserResponse>.Success(response));
    }
}
```

Register Kawa, register the use case, and map it as a Minimal API endpoint:

```csharp
using Kawa.Abstractions;
using Kawa.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddKawa();
builder.Services.AddSingleton<IUseCase<CreateUserRequest, CreateUserResponse>, CreateUserUseCase>();

var app = builder.Build();

app.MapKawaPost<CreateUserRequest, CreateUserResponse>("/users");

app.Run();
```

Then call the endpoint:

```bash
curl -X POST http://localhost:5000/users \
  -H "Content-Type: application/json" \
  -d '{"name":"Louisa"}'
```

## Result to HTTP Mapping

`MapKawaPost<TRequest,TResponse>` resolves the matching `IUseCase<TRequest,TResponse>` from dependency injection, executes it through `UseCaseExecutor`, and converts the result to an ASP.NET Core `IResult`.

Current failure mappings:

| `KawaErrorKind` | HTTP response |
| --- | --- |
| `Validation` | `400 Bad Request` |
| `Unauthorized` | `401 Unauthorized` |
| `Forbidden` | `403 Forbidden` |
| `NotFound` | `404 Not Found` |
| `Conflict` | `409 Conflict` |
| `Unknown` | `500 Problem` |

## Samples

Run the C# sample:

```bash
dotnet run --project samples/Kawa.Sample.CSharp
```

Run the mixed C# host + F# use case sample:

```bash
dotnet run --project samples/Kawa.Sample.Mixed
```

Run the mixed C# host + VB.NET use case sample:

```bash
dotnet run --project samples/Kawa.Sample.VB
```

All samples expose `POST /users`.

## Development

Restore, build, and test:

```bash
dotnet restore Kawa.sln
dotnet build Kawa.sln --no-restore
dotnet test Kawa.sln --no-restore --no-build
```

Run tests with coverage:

```bash
dotnet test Kawa.sln \
  --no-restore \
  --no-build \
  --collect:"XPlat Code Coverage" \
  --results-directory TestResults \
  -- \
  DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura
```

The CI pipeline audits NuGet packages during restore and uploads coverage to Codecov using the repository secret `CODECOV_TOKEN`. Project and patch coverage are configured to target 100%.

To create local NuGet packages:

```bash
bash eng/pack.sh
```

To publish the generated packages to NuGet.org:

```bash
NUGET_API_KEY=... bash eng/push-nuget.sh
```

NuGet packages and GitHub Releases are published by GitHub Actions when a tag
in `vX.Y.Z` format is pushed, for example:

```bash
git tag v0.1.1
git push origin v0.1.1
```

The release workflow uses the repository secret `NUGET_API_KEY`.

See:

- [Design Principles](docs/design-principles.md)
- [設計思想メモ 日本語版](docs/design-principles.ja.md)

## License

Kawa is licensed under the MIT License.
