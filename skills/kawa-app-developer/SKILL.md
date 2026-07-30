---
name: kawa-app-developer
description: Build applications that use the Kawa .NET framework. Use when working in a Kawa-consuming app to add use cases, request/response contracts, KawaResult/KawaError failures, ASP.NET Core Minimal API endpoints with MapKawaPost, OpenAPI/catalog checks, tests, or C#/F#/VB.NET use case boundaries. Do not use for changing the Kawa framework repository itself.
---

# Kawa App Developer

Use this skill when building an application with Kawa. Keep application behavior centered on contracts and use cases; treat HTTP as an outer adapter.

## First Steps

1. Confirm the target project consumes Kawa by checking for one or more of:
   - `PackageReference` to `Kawa.Web`, `Kawa.Core`, or `Kawa.Abstractions`
   - `using Kawa.Web`
   - `using Kawa.Abstractions`
   - `IUseCase<TRequest,TResponse>`
   - `MapKawaPost`
2. Inspect existing project conventions before adding files:

```bash
rg -n "AddKawa|MapKawaPost|IUseCase<|KawaResult|KawaError" .
find . -maxdepth 3 -type d | sort
```

3. Read the relevant reference before editing:
   - `references/app-structure.md` for where files should live.
   - `references/usecase-patterns.md` for C# and cross-language use case patterns.
   - `references/testing.md` for use case, endpoint, OpenAPI, and catalog verification.

## Workflow

For a new Kawa feature, work in this order:

1. Define or locate the request and response contracts.
2. Implement `IUseCase<TRequest,TResponse>`.
3. Return `KawaResult<TResponse>` from application flow.
4. Represent predictable failures with `KawaError` and `KawaErrorKind`.
5. Add `KawaUseCaseAttribute` and `KawaErrorResponseAttribute` when the endpoint is part of the public API.
6. Register the use case through the app's existing convention.
7. Map the HTTP entry point with `MapKawaPost<TUseCase>` or `MapKawaPost<TRequest,TResponse>`.
8. Add focused tests.
9. Verify OpenAPI or `/kawa/catalog.json` when the public API shape, metadata, or error responses changed.

## Rules

- Start from contracts and use cases, not from the endpoint.
- Keep use cases independent from ASP.NET Core, HTTP status codes, `HttpContext`, CLI parser types, Worker SDK types, and RPC context types.
- Use `KawaErrorKind` for predictable application failures; do not return ASP.NET Core `IResult` from a use case.
- Prefer `MapKawaPost<TUseCase>` when the use case type carries exactly one request/response contract.
- Use `MapKawaPost<TRequest,TResponse>` when contracts are explicit, especially in cross-language or split-project designs.
- Keep public request/response contracts C# friendly and serialization-friendly.
- Follow existing application layout when it is already clear; introduce Kawa folders only when they reduce ambiguity.

## Verification

Choose the narrowest useful check first:

```bash
dotnet test
dotnet test --no-restore
dotnet run --project <web-project>
```

For public HTTP API changes, verify the relevant endpoint and, when mapped, `/openapi/v1.json` and `/kawa/catalog.json`.

