---
name: kawa-maintainer
description: Maintain the Kawa .NET framework repository with repo-specific guardrails. Use when working in a Kawa checkout on framework changes, public API docs, OpenAPI/Swagger/ReDoc behavior, API catalog behavior, use case/result/error contracts, Kawa.Web endpoint mapping, release/changelog work, or tests that affect generated-client compatibility.
---

# Kawa Maintainer

Use this skill to make Kawa changes without losing the repository's contract-first boundaries.

## First Steps

1. Confirm the checkout is Kawa by finding `Kawa.sln`, `src/Kawa.Abstractions`, `src/Kawa.Core`, and `src/Kawa.Web`.
2. Check the current branch and worktree state before editing:

```bash
git status --short
git branch --show-current
```

3. Before making framework changes, work on a dedicated branch. If already on a non-main task branch, keep using it. If on `main` or another protected integration branch, create a scoped branch before editing:

```bash
git switch -c codex/<short-task-name>
```

4. Read the current documentation map before deciding where to edit:

```bash
sed -n '1,260p' docs/specification.md
sed -n '1,260p' docs/internal-design.md
```

5. Read this skill's `references/kawa-workflow.md` for the source map, documentation map, and change routing rules.

6. If either specification file is absent, treat that as documentation drift. Fall back to `README.md`, `docs/design-principles.md`, and `docs/rails-like-conventions.md`, then include restoring the missing specification docs in the work.

7. Keep the scope narrow: change framework behavior, public docs, tests, or release metadata only when the task actually requires it.

## Classify The Change

Before editing, classify the work:

- Public contract: `Kawa.Abstractions`, package APIs, endpoint mapping APIs, route defaults, HTTP result mapping, API catalog shape, OpenAPI schema/operation behavior, Swagger/ReDoc routes.
- Internal implementation: executor flow, mapper internals, XML documentation parsing, OpenAPI transformers, tests, packaging scripts.
- Documentation/release: README, docs, changelogs, package metadata, GitHub release/NuGet workflow.

Public contract changes need `docs/specification.md`, `docs/specification.ja.md`, and changelog attention. Internal boundary changes need `docs/internal-design.md` and `docs/internal-design.ja.md` attention. OpenAPI/catalog changes need generated-client compatibility attention.

## Kawa Boundaries

- Keep `Kawa.Abstractions` transport-independent. Do not add ASP.NET Core dependencies there.
- Keep `Kawa.Core` transport-independent. It may depend on `Kawa.Abstractions`, not `Kawa.Web`.
- Keep `Kawa.Web` responsible for ASP.NET Core Minimal API integration, HTTP mapping, API catalog endpoints, OpenAPI, Swagger, and ReDoc.
- Keep use cases free of HTTP status codes, ASP.NET Core types, RPC context types, CLI parser types, and Worker SDK types.
- Treat future RPC, CLI, and Worker adapters as design direction unless the repo has implemented packages for them.

## OpenAPI And Catalog Guardrails

When changing OpenAPI, Swagger, ReDoc, API catalog, endpoint metadata, request/response contracts, or error response metadata:

1. Read `docs/specification.md` sections "API Catalog" and "OpenAPI, Swagger, and ReDoc".
2. Read `docs/internal-design.md` sections "API Catalog Generation", "OpenAPI Integration", and "XML Documentation Schema Enrichment".
3. Inspect `src/Kawa.Web/KawaServiceCollectionExtensions.cs` before changing OpenAPI registration.
4. Inspect `src/Kawa.Web/KawaEndpointRouteBuilderExtensions.cs` before changing endpoint metadata.
5. Inspect `src/Kawa.Web/KawaOpenApiOperationTransformer.cs` and `src/Kawa.Web/KawaOpenApiXmlDocumentationSchemaTransformer.cs` before changing generated OpenAPI behavior.
6. Preserve nested contract schema reference IDs based on full nested type names unless intentionally making a breaking change.
7. Keep HTTP mapper behavior, OpenAPI metadata, and specification docs aligned in the same change.
8. Add or update tests under `tests/Kawa.Web.Tests` for generated-client-facing behavior.

## Documentation And Changelog

- Update `CHANGELOG.md` and `CHANGELOG.ja.md` when a user-visible change is added.
- Update `docs/specification.md` and `docs/specification.ja.md` when public behavior changes.
- Update `docs/internal-design.md` and `docs/internal-design.ja.md` when implementation boundaries, extension guidance, or maintainer rules change.
- Keep English and Japanese docs aligned by section order.
- Keep Mermaid diagrams and sequence diagrams aligned with the behavior they describe when execution flow, dependencies, catalog generation, or OpenAPI generation changes.
- Do not describe future transports as implemented behavior.

## Verification

Choose the narrowest useful verification first, then run broader checks when public behavior changes.

Common commands:

```bash
dotnet test Kawa.sln --no-restore --disable-build-servers
dotnet build Kawa.sln --no-restore --disable-build-servers
dotnet restore Kawa.sln --disable-build-servers
```

If `--no-restore` fails because assets are stale, run restore once and retry the original command.

For OpenAPI or generated-client-facing changes, prefer targeted tests in `tests/Kawa.Web.Tests` first, then run the solution tests.

## Extra Reference

Read `references/kawa-workflow.md` when you need a compact checklist of source files, tests, and release commands for Kawa maintenance.
