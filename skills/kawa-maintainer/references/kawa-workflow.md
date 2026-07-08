# Kawa Workflow Reference

## Source Map

- `src/Kawa.Abstractions`: public use case, result, error, catalog, and mapper contracts.
- `src/Kawa.Core`: transport-independent execution.
- `src/Kawa.Web`: ASP.NET Core Minimal API integration, HTTP mapping, API catalog, OpenAPI, Swagger, and ReDoc.
- `src/Kawa.FSharp`: F# helper surface.
- `tests/Kawa.Core.Tests`: transport-independent behavior tests.
- `tests/Kawa.Web.Tests`: web integration, OpenAPI, catalog, and language boundary tests.
- `samples/`: C#, F#, and VB.NET usage samples.

## Branching

- Make framework changes on a dedicated branch.
- If the checkout is on `main` or another protected integration branch, create `codex/<short-task-name>` before editing.
- If the checkout is already on a non-main task branch, continue there unless the user asks for a new branch.
- Keep documentation-only plans or specs on branches too when they are part of repository development work.

## OpenAPI Hotspots

- `KawaServiceCollectionExtensions.AddKawaWeb`: OpenAPI registration, schema reference IDs, operation/schema transformers.
- `KawaEndpointRouteBuilderExtensions.MapKawaPost`: endpoint mapping, request/response metadata, use case metadata.
- `KawaOpenApiOperationTransformer`: operation metadata enrichment.
- `KawaOpenApiXmlDocumentationSchemaTransformer`: XML summary to schema description enrichment.
- `MapKawaPostTests`: endpoint metadata and schema reference regression coverage.
- `KawaOpenApiXmlDocumentationTests`: XML documentation enrichment coverage.

## Release And Packaging

- Update both `CHANGELOG.md` and `CHANGELOG.ja.md`.
- Build packages with `bash eng/pack.sh`.
- Release tags use `vX.Y.Z`.
- GitHub Actions publish NuGet packages and GitHub Releases from release tags.

## Useful Validation

```bash
dotnet restore Kawa.sln --disable-build-servers
dotnet build Kawa.sln --no-restore --disable-build-servers
dotnet test Kawa.sln --no-restore --disable-build-servers
bash eng/pack.sh
```

