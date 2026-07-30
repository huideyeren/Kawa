# Kawa Workflow Reference

## Source Map

- `src/Kawa.Abstractions`: public use case, result, error, catalog, and mapper contracts.
- `src/Kawa.Core`: transport-independent execution.
- `src/Kawa.Web`: ASP.NET Core Minimal API integration, HTTP mapping, API catalog, OpenAPI, Swagger, and ReDoc.
- `src/Kawa.FSharp`: F# helper surface.
- `tests/Kawa.Core.Tests`: transport-independent behavior tests.
- `tests/Kawa.Web.Tests`: web integration, OpenAPI, catalog, and language boundary tests.
- `samples/`: C#, F#, and VB.NET usage samples.

## Documentation Map

- `docs/specification.md`: current user-facing behavior and public compatibility expectations.
- `docs/specification.ja.md`: Japanese user-facing specification; keep section order aligned with English.
- `docs/internal-design.md`: maintainer-facing architecture, data flow, extension boundaries, and test strategy.
- `docs/internal-design.ja.md`: Japanese internal design; keep section order aligned with English.
- `docs/design-principles.md` and `docs/design-principles.ja.md`: background philosophy, not the primary source for current behavior.
- `docs/rails-like-conventions.md` and `docs/rails-like-conventions.ja.md`: convention proposal and future-friendly structure guidance, not a substitute for the specification.

## Change Routing

- Public API, HTTP mapping, API catalog, OpenAPI, Swagger, ReDoc, or package behavior: update both specification files and both changelogs when user-visible.
- Internal dependency direction, execution flow, mapper responsibilities, OpenAPI transformer design, XML documentation enrichment, or extension boundaries: update both internal design files.
- Generated-client-facing behavior: update Web tests, OpenAPI/catalog docs, and any Mermaid diagrams or sequence diagrams that describe the changed flow.
- Documentation-only behavior descriptions: keep English and Japanese documents aligned by headings and examples.

## Branching

- Make framework changes on a dedicated branch.
- If the checkout is on `main` or another protected integration branch, create `codex/<short-task-name>` before editing.
- If the checkout is already on a non-main task branch, continue there unless the user asks for a new branch.
- Keep documentation-only plans or specs on branches too when they are part of repository development work.

## OpenAPI Hotspots

- `docs/specification.md`: "API Catalog" and "OpenAPI, Swagger, and ReDoc".
- `docs/internal-design.md`: "API Catalog Generation", "OpenAPI Integration", and "XML Documentation Schema Enrichment".
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
