# Kawa App Structure

Use the existing application structure first. If no clear Kawa structure exists, prefer one of these shapes.

## Small C# Application

For small applications, one file per use case is acceptable:

```text
UseCases/
  CreateUser.cs
Program.cs
```

The use case file may contain:

- `KawaUseCaseAttribute`
- `KawaErrorResponseAttribute`
- the use case class
- nested `Request` and `Response` records

This shape matches the compact sample style and works well when a feature is still small.

## Larger Application

For larger applications, split contracts, use cases, and web endpoints:

```text
Contracts/
  Users/
    CreateUser.cs
UseCases/
  Users/
    CreateUserUseCase.cs
Endpoints/
  Web/
    UsersEndpoints.cs
Tests/
  Users/
    CreateUserUseCaseTests.cs
```

Read the application in this order:

1. `Contracts/`
2. `UseCases/`
3. `Endpoints/Web/`
4. Tests

## Multi-Language Application

Use project boundaries for language mixing:

```text
MyApp.Contracts/          # preferably C#
MyApp.UseCases.CSharp/
MyApp.UseCases.FSharp/
MyApp.UseCases.VB/
MyApp.Web/                # ASP.NET Core host
```

Keep request and response contracts C# friendly:

- records
- classes
- enums
- simple DTOs
- serialization-friendly property shapes

Do not expose language-specific domain internals directly through public request/response contracts.

## Endpoint Placement

Endpoint files should stay thin:

- declare routes
- call `MapKawaPost`
- attach route groups or authorization as the app already does

Do not put business rules, validation bodies, or transport-independent decisions in endpoint files.

