# Kawa Documentation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add user-facing and maintainer-facing Kawa specification documents in English and Japanese, then link them from the README.

**Architecture:** The documentation is split into public specification documents and internal design documents. Public specification files describe current user-visible behavior; internal design files explain implementation boundaries and maintenance rules. Existing design principle and convention documents remain background material rather than being rewritten.

**Tech Stack:** Markdown, .NET repository documentation, ASP.NET Core Minimal API concepts, Kawa.Abstractions, Kawa.Core, Kawa.Web, Kawa.FSharp.

## Global Constraints

- Do not change Kawa runtime behavior.
- Do not introduce new APIs.
- Do not rewrite the existing design principle or Rails-like convention documents.
- Do not document future transports as implemented features.
- Keep current implemented behavior separate from future design ideas.
- Provide English and Japanese versions for each new document.
- Link the new documents from the README so they are discoverable.
- Use present-tense behavior descriptions for implemented behavior.
- Label future or conceptual behavior explicitly as future work.
- Keep the English and Japanese documents aligned section by section.

---

### Task 1: User-Facing Specification Documents

**Files:**
- Create: `docs/specification.md`
- Create: `docs/specification.ja.md`
- Reference: `README.md`
- Reference: `src/Kawa.Abstractions/IUseCase.cs`
- Reference: `src/Kawa.Abstractions/KawaResult.cs`
- Reference: `src/Kawa.Abstractions/KawaError.cs`
- Reference: `src/Kawa.Abstractions/KawaErrorKind.cs`
- Reference: `src/Kawa.Abstractions/KawaUseCaseAttribute.cs`
- Reference: `src/Kawa.Abstractions/KawaErrorResponseAttribute.cs`
- Reference: `src/Kawa.Web/KawaServiceCollectionExtensions.cs`
- Reference: `src/Kawa.Web/KawaEndpointRouteBuilderExtensions.cs`
- Reference: `src/Kawa.Web/KawaApiCatalogEndpointRouteBuilderExtensions.cs`
- Reference: `src/Kawa.Web/KawaOpenApiEndpointRouteBuilderExtensions.cs`
- Reference: `src/Kawa.Web/KawaOpenApiUiApplicationBuilderExtensions.cs`

**Interfaces:**
- Consumes: The approved design spec at `docs/superpowers/specs/2026-07-09-kawa-documentation-design.md`.
- Produces: English and Japanese public specifications with matching section order and documented user-visible behavior.

- [ ] **Step 1: Re-read the public behavior source files**

Run:

```bash
sed -n '1,220p' src/Kawa.Abstractions/IUseCase.cs
sed -n '1,220p' src/Kawa.Abstractions/KawaResult.cs
sed -n '1,220p' src/Kawa.Abstractions/KawaError.cs
sed -n '1,220p' src/Kawa.Abstractions/KawaErrorKind.cs
sed -n '1,220p' src/Kawa.Abstractions/KawaUseCaseAttribute.cs
sed -n '1,220p' src/Kawa.Abstractions/KawaErrorResponseAttribute.cs
sed -n '1,260p' src/Kawa.Web/KawaServiceCollectionExtensions.cs
sed -n '1,260p' src/Kawa.Web/KawaEndpointRouteBuilderExtensions.cs
sed -n '1,220p' src/Kawa.Web/KawaApiCatalogEndpointRouteBuilderExtensions.cs
sed -n '1,220p' src/Kawa.Web/KawaOpenApiEndpointRouteBuilderExtensions.cs
sed -n '1,220p' src/Kawa.Web/KawaOpenApiUiApplicationBuilderExtensions.cs
```

Expected: The command exits successfully and confirms the current source behavior used by the docs.

- [ ] **Step 2: Create `docs/specification.md`**

Create the English user-facing specification with these sections and content:

```markdown
# Kawa Specification

## Overview

Kawa is a contract-first .NET web framework built as a thin application layer on ASP.NET Core. Kawa centers application flow on request/response contracts and `IUseCase<TRequest,TResponse>` implementations. ASP.NET Core remains responsible for hosting, dependency injection, routing, middleware, authentication, authorization, configuration, logging, and the underlying OpenAPI infrastructure.

The current implemented transport is ASP.NET Core Minimal API integration through `Kawa.Web`. RPC, CLI, and Worker transports are design directions, not implemented package behavior in this repository.

## Package Surface

`Kawa.Abstractions` contains the transport-independent contracts: `IUseCase<TRequest,TResponse>`, `KawaResult<T>`, `KawaError`, `KawaErrorKind`, catalog metadata, and mapper abstractions.

`Kawa.Core` contains transport-independent execution support, currently `UseCaseExecutor`.

`Kawa.Web` contains ASP.NET Core Minimal API integration, HTTP result mapping, API catalog endpoints, OpenAPI integration, and Swagger/ReDoc UI helpers.

`Kawa.FSharp` contains F# helpers for working with Kawa result values.

## UseCase Contract

A Kawa use case implements `IUseCase<TRequest,TResponse>`.

```csharp
public interface IUseCase<TRequest, TResponse>
{
    Task<KawaResult<TResponse>> ExecuteAsync(
        TRequest request,
        CancellationToken cancellationToken = default);
}
```

`TRequest` is the input contract and `TResponse` is the successful output contract. Kawa does not require a specific DTO base class. The recommended convention is to keep request and response contracts close to the use case, commonly as nested `Request` and `Response` record types.

`AddKawaUseCasesFromAssemblies` registers concrete, non-abstract, non-open-generic classes that implement a Kawa use case contract. Each discovered use case is registered as its implemented `IUseCase<TRequest,TResponse>` service.

`MapKawaPost<TUseCase>` requires the use case type to describe exactly one request/response use case contract. `MapKawaPost<TRequest,TResponse>` maps an endpoint by explicit request and response contract types and resolves the matching `IUseCase<TRequest,TResponse>` from dependency injection at request time.

## Result and Error Model

Use cases return `KawaResult<TResponse>`.

`KawaResult<T>.Success(value)` creates a successful result. Successful results have `IsSuccess == true`, `IsFailure == false`, a `Value`, and no `Error`.

`KawaResult<T>.Failure(error)` creates a failed result. Failed results have `IsSuccess == false`, `IsFailure == true`, an `Error`, and no successful value.

`KawaError` represents a predictable application error with a `KawaErrorKind` and a human-readable message.

The current error kinds are:

| Kind | Meaning |
| --- | --- |
| `Validation` | The request is syntactically accepted but violates application validation rules. |
| `Unauthorized` | The caller is not authenticated. |
| `Forbidden` | The caller is authenticated but not allowed to perform the operation. |
| `NotFound` | The requested resource or target does not exist. |
| `Conflict` | The operation conflicts with current application state. |
| `Unknown` | The failure does not fit another known category. |

## Web Integration

Register the default Kawa services with `AddKawa()`:

```csharp
builder.Services.AddKawa();
```

`AddKawa()` registers `UseCaseExecutor`, the HTTP success mapper, the HTTP error mapper, and the HTTP transport mapper.

Register use cases from assemblies with `AddKawaUseCasesFromAssemblies`:

```csharp
builder.Services.AddKawaUseCasesFromAssemblies(typeof(CreateUser).Assembly);
```

Register Kawa's OpenAPI conventions with `AddKawaWeb()`:

```csharp
builder.Services.AddKawaWeb();
```

Map a POST endpoint from a use case type:

```csharp
app.MapKawaPost<CreateUser>("/users");
```

Map a POST endpoint from explicit request and response contract types:

```csharp
app.MapKawaPost<CreateUser.Request, CreateUser.Response>("/users");
```

## HTTP Mapping

`MapKawaPost` binds the JSON request body, resolves the matching use case from dependency injection, executes it through `UseCaseExecutor`, and maps the `KawaResult<TResponse>` to an ASP.NET Core `IResult`.

Successful responses map to `200 OK` with the response contract serialized as JSON.

Failure responses map as follows:

| `KawaErrorKind` | HTTP response |
| --- | --- |
| `Validation` | `400 Bad Request` with `KawaError` JSON |
| `Unauthorized` | `401 Unauthorized` |
| `Forbidden` | `403 Forbidden` |
| `NotFound` | `404 Not Found` with `KawaError` JSON |
| `Conflict` | `409 Conflict` with `KawaError` JSON |
| `Unknown` | `500 Internal Server Error` as `ProblemDetails` |

## API Catalog

`MapKawaApiCatalog()` maps the Kawa API catalog endpoint. The default route is `/kawa/catalog.json`.

The catalog is generated from endpoint metadata for mapped Kawa use cases. It includes use case metadata, request and response contract type names, and error response metadata.

Use `KawaUseCaseAttribute` to describe transport-independent use case metadata:

```csharp
[KawaUseCase(
    "users.create",
    Summary = "Create user",
    Description = "Creates a user account.",
    Version = "v1",
    Tags = new[] { "Users" })]
```

Use `KawaErrorResponseAttribute` to document expected error responses:

```csharp
[KawaErrorResponse(KawaErrorKind.Validation, Description = "Name is required.")]
```

The catalog is transport-independent. `Kawa.Web` exposes it over HTTP, and future adapters can use the same metadata without making the use case depend on HTTP.

## OpenAPI, Swagger, and ReDoc

`MapKawaOpenApi()` maps the OpenAPI document endpoint. The default route is `/openapi/v1.json`.

`AddKawaWeb()` configures Kawa OpenAPI behavior. Nested contract types such as `CreateUser.Request` and `CreateUser.Response` use schema reference IDs based on their full nested type names, with `+` replaced by `.`, so generated clients do not confuse same-named nested contracts from different use cases.

Kawa maps use case metadata to OpenAPI endpoint metadata when endpoints are mapped through `MapKawaPost<TUseCase>`.

When a contract assembly emits an XML documentation file next to its assembly, Kawa uses type and property summaries as OpenAPI schema descriptions. Projects that define documented contracts should enable:

```xml
<GenerateDocumentationFile>true</GenerateDocumentationFile>
```

`MapKawaSwagger()` maps Swagger UI. The default route is `/swagger`.

`MapKawaReDoc()` maps ReDoc. The default route is `/redoc`.

Swagger and ReDoc are application middleware. The recommended default is to map them only in development. Production exposure should be an explicit application decision.

## Multi-Language Boundary

Kawa supports use cases implemented from multiple CLR languages as long as the public boundary remains compatible with Kawa's C# friendly contracts.

The repository includes C#, F#, and VB.NET samples. The samples keep the ASP.NET Core host in C# while allowing use cases to live in language-specific projects.

Public request and response contracts should remain simple .NET types that are natural for C#, ASP.NET Core serialization, OpenAPI generation, and other CLR languages to consume.

## Compatibility and Stability Notes

The current stable behavior is the public API surface in the Kawa packages, the use case execution contract, the documented HTTP result mapping, the API catalog shape, and the OpenAPI integration behavior described above.

Kawa does not currently provide implemented RPC, CLI, Worker, Controller, EF Core, authentication, authorization, validation framework, code generation, or project template packages. Those areas may be explored later, but applications should not depend on them as current Kawa behavior.

Because Kawa builds on ASP.NET Core, application authors remain responsible for host configuration, middleware ordering, authentication and authorization policies, production documentation exposure, logging, and deployment settings.
```

- [ ] **Step 3: Create `docs/specification.ja.md`**

Create the Japanese user-facing specification with the same section order and equivalent technical content:

```markdown
# Kawa 仕様書

## 概要

Kawa は、ASP.NET Core の上に薄いアプリケーション層として構築する contract-first の .NET Web フレームワークです。Kawa は、request/response contract と `IUseCase<TRequest,TResponse>` 実装をアプリケーションの中心に置きます。Hosting、dependency injection、routing、middleware、authentication、authorization、configuration、logging、基盤となる OpenAPI infrastructure は ASP.NET Core の責務です。

現在実装済みの transport は、`Kawa.Web` による ASP.NET Core Minimal API 統合です。RPC、CLI、Worker transport は設計上の方向性であり、このリポジトリで実装済みの package behavior ではありません。

## Package Surface

`Kawa.Abstractions` は transport-independent な contract を含みます。主な型は `IUseCase<TRequest,TResponse>`、`KawaResult<T>`、`KawaError`、`KawaErrorKind`、catalog metadata、mapper abstractions です。

`Kawa.Core` は transport-independent な実行支援を含みます。現在の中心は `UseCaseExecutor` です。

`Kawa.Web` は ASP.NET Core Minimal API 統合、HTTP result mapping、API catalog endpoints、OpenAPI integration、Swagger/ReDoc UI helpers を含みます。

`Kawa.FSharp` は Kawa result values を扱うための F# helper を含みます。

## UseCase Contract

Kawa の use case は `IUseCase<TRequest,TResponse>` を実装します。

```csharp
public interface IUseCase<TRequest, TResponse>
{
    Task<KawaResult<TResponse>> ExecuteAsync(
        TRequest request,
        CancellationToken cancellationToken = default);
}
```

`TRequest` は入力 contract、`TResponse` は成功時の出力 contract です。Kawa は特定の DTO base class を要求しません。推奨規約は、request と response contract を use case の近くに置くことです。一般的には nested `Request` / `Response` record type として定義します。

`AddKawaUseCasesFromAssemblies` は、Kawa use case contract を実装する concrete、non-abstract、non-open-generic class を登録します。発見された use case は、実装している `IUseCase<TRequest,TResponse>` service として登録されます。

`MapKawaPost<TUseCase>` は、use case type がちょうど 1 つの request/response use case contract を表すことを要求します。`MapKawaPost<TRequest,TResponse>` は明示的な request/response contract type で endpoint を map し、request time に対応する `IUseCase<TRequest,TResponse>` を dependency injection から解決します。

## Result and Error Model

Use case は `KawaResult<TResponse>` を返します。

`KawaResult<T>.Success(value)` は成功 result を作成します。成功 result は `IsSuccess == true`、`IsFailure == false`、`Value` を持ち、`Error` を持ちません。

`KawaResult<T>.Failure(error)` は失敗 result を作成します。失敗 result は `IsSuccess == false`、`IsFailure == true`、`Error` を持ち、成功 value を持ちません。

`KawaError` は、`KawaErrorKind` と人間が読める message を持つ、予測可能な application error を表します。

現在の error kind は次の通りです。

| Kind | 意味 |
| --- | --- |
| `Validation` | request は構文上受け付けられたが、application validation rule に違反しています。 |
| `Unauthorized` | caller が authenticated ではありません。 |
| `Forbidden` | caller は authenticated ですが、その operation を実行できません。 |
| `NotFound` | 要求された resource または target が存在しません。 |
| `Conflict` | operation が現在の application state と衝突しています。 |
| `Unknown` | 他の既知 category に当てはまらない failure です。 |

## Web Integration

default の Kawa service は `AddKawa()` で登録します。

```csharp
builder.Services.AddKawa();
```

`AddKawa()` は `UseCaseExecutor`、HTTP success mapper、HTTP error mapper、HTTP transport mapper を登録します。

Use case は `AddKawaUseCasesFromAssemblies` で assembly から登録します。

```csharp
builder.Services.AddKawaUseCasesFromAssemblies(typeof(CreateUser).Assembly);
```

Kawa の OpenAPI convention は `AddKawaWeb()` で登録します。

```csharp
builder.Services.AddKawaWeb();
```

Use case type から POST endpoint を map します。

```csharp
app.MapKawaPost<CreateUser>("/users");
```

明示的な request/response contract type から POST endpoint を map します。

```csharp
app.MapKawaPost<CreateUser.Request, CreateUser.Response>("/users");
```

## HTTP Mapping

`MapKawaPost` は JSON request body を bind し、対応する use case を dependency injection から解決し、`UseCaseExecutor` で実行し、`KawaResult<TResponse>` を ASP.NET Core `IResult` に変換します。

成功 response は、response contract を JSON として serialize した `200 OK` に map されます。

失敗 response は次のように map されます。

| `KawaErrorKind` | HTTP response |
| --- | --- |
| `Validation` | `KawaError` JSON を持つ `400 Bad Request` |
| `Unauthorized` | `401 Unauthorized` |
| `Forbidden` | `403 Forbidden` |
| `NotFound` | `KawaError` JSON を持つ `404 Not Found` |
| `Conflict` | `KawaError` JSON を持つ `409 Conflict` |
| `Unknown` | `ProblemDetails` としての `500 Internal Server Error` |

## API Catalog

`MapKawaApiCatalog()` は Kawa API catalog endpoint を map します。default route は `/kawa/catalog.json` です。

catalog は、mapped Kawa use case の endpoint metadata から生成されます。use case metadata、request/response contract type name、error response metadata を含みます。

Transport-independent な use case metadata は `KawaUseCaseAttribute` で記述します。

```csharp
[KawaUseCase(
    "users.create",
    Summary = "Create user",
    Description = "Creates a user account.",
    Version = "v1",
    Tags = new[] { "Users" })]
```

想定される error response は `KawaErrorResponseAttribute` で記述します。

```csharp
[KawaErrorResponse(KawaErrorKind.Validation, Description = "Name is required.")]
```

catalog は transport-independent です。`Kawa.Web` はこれを HTTP で公開します。将来の adapter は、use case を HTTP に依存させずに同じ metadata を利用できます。

## OpenAPI, Swagger, and ReDoc

`MapKawaOpenApi()` は OpenAPI document endpoint を map します。default route は `/openapi/v1.json` です。

`AddKawaWeb()` は Kawa の OpenAPI behavior を設定します。`CreateUser.Request` や `CreateUser.Response` のような nested contract type は、full nested type name を元にした schema reference ID を使います。このとき `+` は `.` に置き換えます。これにより、generated client が別 use case の同名 nested contract を取り違えにくくなります。

Endpoint が `MapKawaPost<TUseCase>` で map される場合、Kawa は use case metadata を OpenAPI endpoint metadata に map します。

Contract assembly が assembly の隣に XML documentation file を出力している場合、Kawa は type と property の summary を OpenAPI schema description として使用します。Documented contract を定義する project では次を有効にしてください。

```xml
<GenerateDocumentationFile>true</GenerateDocumentationFile>
```

`MapKawaSwagger()` は Swagger UI を map します。default route は `/swagger` です。

`MapKawaReDoc()` は ReDoc を map します。default route は `/redoc` です。

Swagger と ReDoc は application middleware です。推奨 default は development でのみ map することです。Production で公開する場合は、application の明示的な判断として扱ってください。

## Multi-Language Boundary

Kawa は、public boundary が Kawa の C# friendly contract と互換である限り、複数の CLR language で実装された use case を扱えます。

このリポジトリには C#、F#、VB.NET sample が含まれます。Sample は ASP.NET Core host を C# に置きつつ、use case を language-specific project に置けることを示します。

Public request/response contract は、C#、ASP.NET Core serialization、OpenAPI generation、他の CLR language から自然に扱える単純な .NET type にしてください。

## Compatibility and Stability Notes

現在の stable behavior は、Kawa package の public API surface、use case execution contract、ここで記述した HTTP result mapping、API catalog shape、OpenAPI integration behavior です。

Kawa は現在、RPC、CLI、Worker、Controller、EF Core、authentication、authorization、validation framework、code generation、project template package を提供していません。これらは将来検討される可能性がありますが、現在の Kawa behavior として依存しないでください。

Kawa は ASP.NET Core の上に構築されます。そのため、host configuration、middleware ordering、authentication/authorization policy、production documentation exposure、logging、deployment settings は application author の責務です。
```

- [ ] **Step 4: Verify the public specification files**

Run:

```bash
rg -n "TB[D]|TO[D]O|[Pp]laceholder|implemented package behavior|現在実装済み" docs/specification.md docs/specification.ja.md
```

Expected: No draft-marker matches. Matches for "implemented package behavior" and "現在実装済み" are acceptable because they separate current behavior from future design.

- [ ] **Step 5: Commit Task 1**

Run:

```bash
git add docs/specification.md docs/specification.ja.md
git commit -m "Add Kawa public specification docs"
```

Expected: Commit succeeds with only the two public specification files staged.

---

### Task 2: Maintainer-Facing Internal Design Documents

**Files:**
- Create: `docs/internal-design.md`
- Create: `docs/internal-design.ja.md`
- Reference: `src/Kawa.Core/UseCaseExecutor.cs`
- Reference: `src/Kawa.Web/KawaServiceCollectionExtensions.cs`
- Reference: `src/Kawa.Web/KawaEndpointRouteBuilderExtensions.cs`
- Reference: `src/Kawa.Web/KawaHttpSuccessMapper.cs`
- Reference: `src/Kawa.Web/KawaHttpErrorMapper.cs`
- Reference: `src/Kawa.Web/KawaHttpTransportMapper.cs`
- Reference: `src/Kawa.Web/KawaApiCatalogEndpointRouteBuilderExtensions.cs`
- Reference: `src/Kawa.Web/KawaOpenApiOperationTransformer.cs`
- Reference: `src/Kawa.Web/KawaOpenApiXmlDocumentationSchemaTransformer.cs`
- Reference: `tests/Kawa.Core.Tests`
- Reference: `tests/Kawa.Web.Tests`

**Interfaces:**
- Consumes: The public specification documents from Task 1.
- Produces: English and Japanese internal design documents with matching section order and maintenance guidance.

- [ ] **Step 1: Re-read the implementation source files**

Run:

```bash
sed -n '1,220p' src/Kawa.Core/UseCaseExecutor.cs
sed -n '1,260p' src/Kawa.Web/KawaServiceCollectionExtensions.cs
sed -n '1,260p' src/Kawa.Web/KawaEndpointRouteBuilderExtensions.cs
sed -n '1,220p' src/Kawa.Web/KawaHttpSuccessMapper.cs
sed -n '1,220p' src/Kawa.Web/KawaHttpErrorMapper.cs
sed -n '1,220p' src/Kawa.Web/KawaHttpTransportMapper.cs
sed -n '1,220p' src/Kawa.Web/KawaApiCatalogEndpointRouteBuilderExtensions.cs
sed -n '1,240p' src/Kawa.Web/KawaOpenApiOperationTransformer.cs
sed -n '1,280p' src/Kawa.Web/KawaOpenApiXmlDocumentationSchemaTransformer.cs
find tests/Kawa.Core.Tests tests/Kawa.Web.Tests -maxdepth 2 -type f | sort
```

Expected: The command exits successfully and confirms the current implementation boundaries used by the docs.

- [ ] **Step 2: Create `docs/internal-design.md`**

Create the English maintainer-facing internal design document with these sections and content:

```markdown
# Kawa Internal Design

## Architecture Overview

Kawa is split by transport boundary.

`Kawa.Abstractions` owns contracts that must not depend on ASP.NET Core. This includes use case contracts, result/error types, catalog metadata, and mapper abstractions.

`Kawa.Core` owns transport-independent execution. It can depend on `Kawa.Abstractions`, but it should not depend on `Kawa.Web`.

`Kawa.Web` owns ASP.NET Core Minimal API integration. It adapts Kawa contracts into HTTP endpoints, HTTP result mapping, API catalog endpoints, OpenAPI metadata, and documentation UI middleware.

`Kawa.FSharp` owns language helpers. It should not become a transport adapter.

Dependency direction should stay one-way: Web depends on Core and Abstractions; Core depends on Abstractions; Abstractions does not depend on Web.

## Public Contract Ownership

Types in `Kawa.Abstractions` are the most stable public contract surface. Changes to `IUseCase<TRequest,TResponse>`, `KawaResult<T>`, `KawaError`, `KawaErrorKind`, metadata attributes, catalog records, or mapper interfaces should be treated as user-visible compatibility changes.

Types in `Kawa.Web` are public integration APIs for ASP.NET Core applications. Changes to service registration methods, endpoint mapping methods, route defaults, HTTP mappings, OpenAPI behavior, or Swagger/ReDoc helpers are user-visible.

Internal implementation can change, but maintainers should preserve documented behavior unless the change is intentionally breaking and reflected in changelog and versioning.

## UseCase Discovery and Registration

`AddKawaUseCasesFromAssemblies` scans supplied assemblies for concrete classes. A type is eligible when it is a class, not abstract, and does not contain generic parameters.

Eligible classes are checked with `KawaUseCaseContract.IsUseCaseType`. For each use case type, Kawa builds:

- a `KawaUseCaseCatalogEntry` from `KawaUseCaseCatalog.FromUseCaseType`
- a `KawaUseCaseContract` from `KawaUseCaseContract.FromUseCaseType`

The catalog entry is registered as a singleton. The use case implementation is registered as a singleton for the discovered `IUseCase<TRequest,TResponse>` interface.

This means endpoint execution resolves use cases through their interface contract, while catalog and OpenAPI metadata can still be derived from the concrete use case type.

## Execution Flow

`MapKawaPost<TUseCase>` reads the request/response contract from the concrete use case type, then delegates to the generic mapping path.

`MapKawaPost<TRequest,TResponse>` maps an ASP.NET Core POST endpoint. The generated handler:

1. binds the request body as `TRequest`
2. resolves `IUseCase<TRequest,TResponse>` from dependency injection
3. resolves `UseCaseExecutor`
4. resolves `ITransportMapper<IResult>`
5. executes the use case through the executor
6. maps the `KawaResult<TResponse>` into an ASP.NET Core `IResult`

`UseCaseExecutor` is intentionally thin. It calls `ExecuteAsync` on the supplied use case and passes through the cancellation token.

## HTTP Transport Mapping

HTTP mapping is split into small services.

`KawaHttpSuccessMapper` maps successful `KawaResult<T>` values to HTTP success responses.

`KawaHttpErrorMapper` maps `KawaError` values to HTTP failure responses.

`KawaHttpTransportMapper` chooses the success or error mapper based on `KawaResult<T>.IsSuccess`.

This split keeps the public transport mapper abstraction usable by future transports while allowing the Web package to own HTTP-specific response choices.

## API Catalog Generation

`MapKawaApiCatalog()` maps a GET endpoint at the configured catalog route. The default route is `/kawa/catalog.json`.

At request time, the endpoint reads `EndpointDataSource`, finds endpoint metadata of type `KawaUseCaseCatalogEntry`, and converts those entries into `KawaApiCatalog`.

This design means the catalog describes mapped endpoints, not every use case registered in dependency injection. A registered use case that is never mapped should not appear in the HTTP catalog.

The catalog remains transport-independent because the metadata comes from Kawa use case attributes and Kawa contract types, not from HTTP-specific DTOs.

## OpenAPI Integration

`AddKawaWeb()` calls `AddOpenApi` and configures Kawa-specific OpenAPI behavior:

- `CreateSchemaReferenceId` is replaced for nested contract type naming.
- `KawaOpenApiOperationTransformer` enriches operations from Kawa metadata.
- `KawaOpenApiXmlDocumentationSchemaTransformer` enriches schemas from XML documentation.

The nested schema reference rule exists because Kawa contracts commonly use nested `Request` and `Response` type names. Default short schema IDs can collide across use cases. Kawa uses the nested type full name with `+` replaced by `.` so generated clients can bind endpoints to the correct request and response schemas.

OpenAPI metadata attached by `MapKawaPost` should remain consistent with the actual HTTP mapper behavior. If a status code or response body changes in the mapper, update OpenAPI metadata and tests in the same change.

## XML Documentation Schema Enrichment

`KawaOpenApiXmlDocumentationSchemaTransformer` reads XML documentation files emitted next to contract assemblies. It maps type and property summaries to schema descriptions when a schema does not already have a description.

The transformer should fail closed. Missing XML documentation, malformed XML, or absent summary entries should result in no additional description rather than endpoint generation failure.

This behavior keeps XML comments useful for generated clients without making XML documentation a runtime requirement.

## Test Strategy

Core tests should cover transport-independent behavior, including `KawaResult<T>`, `UseCaseExecutor`, and API catalog conversion.

Web tests should cover endpoint mapping, result conversion, service registration, OpenAPI operation metadata, schema reference naming, XML documentation enrichment, and language boundary behavior.

Any change that affects generated clients should include OpenAPI regression tests. Schema ID and schema description behavior are generated-client contract concerns, not cosmetic documentation concerns.

Any change that affects public HTTP behavior should update both mapper tests and OpenAPI metadata tests.

## Extension Boundaries

Future RPC, CLI, and Worker adapters should depend on `Kawa.Abstractions` and `Kawa.Core` where possible. They should not depend on `Kawa.Web` for transport-independent behavior.

New transports should provide their own mapper implementations instead of reusing HTTP result types.

Use case contracts must remain transport-independent. Do not require use cases to reference ASP.NET Core, HTTP status codes, MagicOnion context, CLI parser types, or Worker SDK types.

Future adapter documentation should clearly distinguish implemented package behavior from design direction.
```

- [ ] **Step 3: Create `docs/internal-design.ja.md`**

Create the Japanese maintainer-facing internal design document with the same section order and equivalent technical content:

```markdown
# Kawa 内部設計

## Architecture Overview

Kawa は transport boundary によって分割します。

`Kawa.Abstractions` は ASP.NET Core に依存してはいけない contract を所有します。Use case contract、result/error type、catalog metadata、mapper abstractions が含まれます。

`Kawa.Core` は transport-independent な実行を所有します。`Kawa.Abstractions` には依存できますが、`Kawa.Web` に依存してはいけません。

`Kawa.Web` は ASP.NET Core Minimal API 統合を所有します。Kawa contract を HTTP endpoint、HTTP result mapping、API catalog endpoint、OpenAPI metadata、documentation UI middleware に適合させます。

`Kawa.FSharp` は language helper を所有します。Transport adapter にならないようにします。

Dependency direction は一方向に保ちます。Web は Core と Abstractions に依存し、Core は Abstractions に依存し、Abstractions は Web に依存しません。

## Public Contract Ownership

`Kawa.Abstractions` の型は、最も安定させるべき public contract surface です。`IUseCase<TRequest,TResponse>`、`KawaResult<T>`、`KawaError`、`KawaErrorKind`、metadata attributes、catalog records、mapper interfaces への変更は user-visible compatibility change として扱います。

`Kawa.Web` の型は ASP.NET Core application 向けの public integration API です。Service registration methods、endpoint mapping methods、route defaults、HTTP mappings、OpenAPI behavior、Swagger/ReDoc helpers への変更は user-visible です。

Internal implementation は変更できますが、maintainer は、意図的な breaking change として changelog と versioning に反映する場合を除き、documented behavior を維持してください。

## UseCase Discovery and Registration

`AddKawaUseCasesFromAssemblies` は、指定された assembly から concrete class を scan します。対象になる型は class、not abstract、generic parameter を含まない型です。

対象型は `KawaUseCaseContract.IsUseCaseType` で確認されます。各 use case type について、Kawa は次を作成します。

- `KawaUseCaseCatalog.FromUseCaseType` から `KawaUseCaseCatalogEntry`
- `KawaUseCaseContract.FromUseCaseType` から `KawaUseCaseContract`

Catalog entry は singleton として登録されます。Use case implementation は、発見された `IUseCase<TRequest,TResponse>` interface の singleton として登録されます。

これにより、endpoint execution は interface contract 経由で use case を解決しつつ、catalog と OpenAPI metadata は concrete use case type から導出できます。

## Execution Flow

`MapKawaPost<TUseCase>` は concrete use case type から request/response contract を読み取り、generic mapping path に委譲します。

`MapKawaPost<TRequest,TResponse>` は ASP.NET Core POST endpoint を map します。生成される handler は次の順に処理します。

1. request body を `TRequest` として bind する
2. dependency injection から `IUseCase<TRequest,TResponse>` を解決する
3. `UseCaseExecutor` を解決する
4. `ITransportMapper<IResult>` を解決する
5. executor 経由で use case を実行する
6. `KawaResult<TResponse>` を ASP.NET Core `IResult` に map する

`UseCaseExecutor` は意図的に薄く保ちます。渡された use case の `ExecuteAsync` を呼び出し、cancellation token を渡します。

## HTTP Transport Mapping

HTTP mapping は小さな service に分割します。

`KawaHttpSuccessMapper` は成功した `KawaResult<T>` value を HTTP success response に map します。

`KawaHttpErrorMapper` は `KawaError` value を HTTP failure response に map します。

`KawaHttpTransportMapper` は `KawaResult<T>.IsSuccess` に基づき、success mapper または error mapper を選びます。

この分割により、public transport mapper abstraction を将来の transport でも利用しやすくしながら、HTTP-specific response choice は Web package が所有できます。

## API Catalog Generation

`MapKawaApiCatalog()` は設定された catalog route に GET endpoint を map します。default route は `/kawa/catalog.json` です。

Request time に endpoint は `EndpointDataSource` を読み取り、`KawaUseCaseCatalogEntry` 型の endpoint metadata を見つけ、それらの entry を `KawaApiCatalog` に変換します。

この設計では、catalog は dependency injection に登録された全 use case ではなく、mapped endpoint を表します。登録済みでも map されていない use case は HTTP catalog に出るべきではありません。

Catalog は transport-independent のままです。Metadata は Kawa use case attributes と Kawa contract type から来るものであり、HTTP-specific DTO から来るものではありません。

## OpenAPI Integration

`AddKawaWeb()` は `AddOpenApi` を呼び出し、Kawa-specific な OpenAPI behavior を設定します。

- nested contract type naming のために `CreateSchemaReferenceId` を置き換えます。
- `KawaOpenApiOperationTransformer` が Kawa metadata から operation を enrich します。
- `KawaOpenApiXmlDocumentationSchemaTransformer` が XML documentation から schema を enrich します。

Nested schema reference rule が必要なのは、Kawa contract が nested `Request` / `Response` type name をよく使うためです。Default の短い schema ID は use case 間で衝突する可能性があります。Kawa は nested type full name を使い、`+` を `.` に置き換えることで、generated client が endpoint を正しい request/response schema に bind できるようにします。

`MapKawaPost` が付与する OpenAPI metadata は、実際の HTTP mapper behavior と一貫している必要があります。Status code や response body が mapper 側で変わる場合は、OpenAPI metadata と tests も同じ変更で更新してください。

## XML Documentation Schema Enrichment

`KawaOpenApiXmlDocumentationSchemaTransformer` は contract assembly の隣に出力された XML documentation file を読み取ります。Schema がまだ description を持たない場合、type と property の summary を schema description に map します。

Transformer は fail closed にしてください。XML documentation がない、XML が壊れている、summary entry がない場合は、endpoint generation failure ではなく、追加 description なしとして扱います。

この behavior により、XML comment は generated client に有用な情報を提供できますが、XML documentation を runtime requirement にはしません。

## Test Strategy

Core tests は transport-independent behavior を cover します。対象は `KawaResult<T>`、`UseCaseExecutor`、API catalog conversion です。

Web tests は endpoint mapping、result conversion、service registration、OpenAPI operation metadata、schema reference naming、XML documentation enrichment、language boundary behavior を cover します。

Generated client に影響する変更には OpenAPI regression tests を含めてください。Schema ID と schema description behavior は見た目の documentation ではなく、generated-client contract に関わるものです。

Public HTTP behavior に影響する変更では、mapper tests と OpenAPI metadata tests の両方を更新してください。

## Extension Boundaries

将来の RPC、CLI、Worker adapter は、可能な限り `Kawa.Abstractions` と `Kawa.Core` に依存してください。Transport-independent behavior のために `Kawa.Web` に依存してはいけません。

新しい transport は、HTTP result type を再利用するのではなく、その transport 用の mapper implementation を提供してください。

Use case contract は transport-independent のままにします。Use case に ASP.NET Core、HTTP status code、MagicOnion context、CLI parser type、Worker SDK type への参照を要求してはいけません。

将来の adapter documentation では、implemented package behavior と design direction を明確に区別してください。
```

- [ ] **Step 4: Verify the internal design files**

Run:

```bash
rg -n "TB[D]|TO[D]O|[Pp]laceholder|implemented package behavior|現在実装済み" docs/internal-design.md docs/internal-design.ja.md
```

Expected: No draft-marker matches. Matches for "implemented package behavior" are acceptable only when distinguishing implemented behavior from design direction.

- [ ] **Step 5: Commit Task 2**

Run:

```bash
git add docs/internal-design.md docs/internal-design.ja.md
git commit -m "Add Kawa internal design docs"
```

Expected: Commit succeeds with only the two internal design files staged.

---

### Task 3: README Links and Final Validation

**Files:**
- Modify: `README.md`
- Reference: `docs/specification.md`
- Reference: `docs/specification.ja.md`
- Reference: `docs/internal-design.md`
- Reference: `docs/internal-design.ja.md`

**Interfaces:**
- Consumes: Public and internal documents from Tasks 1 and 2.
- Produces: README discoverability and final validation evidence.

- [ ] **Step 1: Add README links**

Modify the `See:` list in `README.md` to include the new specification documents before the design principle links:

```markdown
- [Specification](docs/specification.md)
- [仕様書 日本語版](docs/specification.ja.md)
- [Internal Design](docs/internal-design.md)
- [内部設計 日本語版](docs/internal-design.ja.md)
```

- [ ] **Step 2: Check all new links and draft-marker text**

Run:

```bash
rg -n "Specification|仕様書|Internal Design|内部設計" README.md
rg -n "TB[D]|TO[D]O|[Pp]laceholder" docs/specification.md docs/specification.ja.md docs/internal-design.md docs/internal-design.ja.md README.md
```

Expected: The first command finds the new README links. The second command produces no matches.

- [ ] **Step 3: Check aligned section headings**

Run:

```bash
rg -n "^## " docs/specification.md docs/specification.ja.md docs/internal-design.md docs/internal-design.ja.md
```

Expected: English and Japanese public specification files have the same section count and order. English and Japanese internal design files have the same section count and order.

- [ ] **Step 4: Run repository validation**

Run:

```bash
dotnet test Kawa.sln --no-restore --disable-build-servers
```

Expected: Tests pass. If restore assets are stale, run `dotnet restore Kawa.sln --disable-build-servers`, then rerun the test command.

- [ ] **Step 5: Commit Task 3**

Run:

```bash
git add README.md
git commit -m "Link Kawa specification docs"
```

Expected: Commit succeeds with README changes staged.

- [ ] **Step 6: Final status check**

Run:

```bash
git status --short
git log --oneline -4
```

Expected: Working tree is clean except for intentional untracked files, and the recent commits include the documentation design spec plus the three implementation commits.
