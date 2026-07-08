# Kawa Documentation Design

## Purpose

Kawa currently has a strong README, design principles, and Rails-like convention proposal, but it does not yet have dedicated specification documents. The existing documents explain direction and usage, while the missing layer is a stable reference for what Kawa currently guarantees and how maintainers should evolve the implementation without breaking those guarantees.

This work will add both user-facing and maintainer-facing documentation.

## Goals

- Document Kawa's current public behavior as a user-facing specification.
- Document Kawa's internal architecture and extension boundaries for maintainers.
- Keep current implemented behavior separate from future design ideas.
- Provide English and Japanese versions for each new document.
- Link the new documents from the README so they are discoverable.

## Non-Goals

- Do not change Kawa runtime behavior.
- Do not introduce new APIs.
- Do not rewrite the existing design principle or Rails-like convention documents.
- Do not document future transports as implemented features.

## Proposed Files

Create four documents:

- `docs/specification.md`
- `docs/specification.ja.md`
- `docs/internal-design.md`
- `docs/internal-design.ja.md`

Update `README.md` to link to the new documents alongside the existing changelog, design principles, and convention proposal.

## User-Facing Specification

The user-facing specification is for developers who build applications with Kawa. It should describe the public contract and expected behavior without requiring readers to understand Kawa internals.

Planned sections:

1. Overview
   - What Kawa provides.
   - What remains ASP.NET Core's responsibility.
   - Current scope versus future transport ideas.

2. Package Surface
   - `Kawa.Abstractions`
   - `Kawa.Core`
   - `Kawa.Web`
   - `Kawa.FSharp`

3. UseCase Contract
   - `IUseCase<TRequest,TResponse>`
   - Request and response contract expectations.
   - Concrete use case registration through `AddKawaUseCasesFromAssemblies`.

4. Result and Error Model
   - `KawaResult<T>`
   - `KawaError`
   - `KawaErrorKind`
   - Success and failure conventions.

5. Web Integration
   - `AddKawa`
   - `AddKawaUseCasesFromAssemblies`
   - `AddKawaWeb`
   - `MapKawaPost<TUseCase>`
   - `MapKawaPost<TRequest,TResponse>`

6. HTTP Mapping
   - Successful responses map to `200 OK`.
   - `KawaErrorKind` values map to documented HTTP responses.
   - Unknown failures map to `ProblemDetails`.

7. API Catalog
   - `/kawa/catalog.json`
   - `KawaUseCaseAttribute`
   - `KawaErrorResponseAttribute`
   - Catalog contents and source metadata.

8. OpenAPI, Swagger, and ReDoc
   - `/openapi/v1.json`
   - Swagger and ReDoc UI helpers.
   - Development-only UI recommendation.
   - XML documentation enrichment.
   - Nested `Request` and `Response` schema reference naming.

9. Multi-Language Boundary
   - C#, F#, and VB.NET sample positioning.
   - C# friendly public boundaries.

10. Compatibility and Stability Notes
   - Current guarantees.
   - Explicitly unsupported or future areas.

## Maintainer-Facing Internal Design

The internal design document is for contributors who modify Kawa itself. It should explain responsibility boundaries, data flow, and the reasons behind the current implementation structure.

Planned sections:

1. Architecture Overview
   - Package dependency direction.
   - Abstractions/Core/Web/FSharp responsibilities.

2. Public Contract Ownership
   - Public types that require compatibility care.
   - What changes should be treated as user-visible.

3. UseCase Discovery and Registration
   - Concrete class scanning.
   - `IUseCase<TRequest,TResponse>` detection.
   - DI registrations.
   - Catalog entry registrations.

4. Execution Flow
   - Endpoint mapping.
   - Request binding.
   - `UseCaseExecutor`.
   - `ITransportMapper<IResult>`.

5. HTTP Transport Mapping
   - `KawaHttpSuccessMapper`.
   - `KawaHttpErrorMapper`.
   - `KawaHttpTransportMapper`.

6. API Catalog Generation
   - Endpoint metadata collection.
   - Transport-independent metadata.
   - Catalog response generation.

7. OpenAPI Integration
   - `AddOpenApi`.
   - Operation transformer responsibilities.
   - Schema transformer responsibilities.
   - Schema reference ID rules for nested contracts.

8. XML Documentation Schema Enrichment
   - XML file discovery.
   - Type and property summary mapping.
   - Fail-closed behavior when XML docs are absent or malformed.

9. Test Strategy
   - Core behavior tests.
   - Web endpoint tests.
   - OpenAPI/schema regression tests.
   - Language boundary tests.

10. Extension Boundaries
   - How future RPC, CLI, and Worker adapters should depend on Kawa.
   - Rules for keeping Web-specific implementation out of transport-independent contracts.

## Documentation Style

- Use clear specification language.
- Prefer present-tense behavior descriptions for implemented behavior.
- Label future or conceptual behavior explicitly as future work.
- Keep the English and Japanese documents aligned section by section.
- Use examples only when they clarify a contract or behavior.

## Validation

Because this is documentation-only work, validation should include:

- Reviewing links from `README.md`.
- Checking that the new documents contain no placeholder sections.
- Checking that user-facing behavior matches the current source files.
- Running an appropriate lightweight build or test command if feasible, primarily to ensure the repository is still healthy after documentation edits.

