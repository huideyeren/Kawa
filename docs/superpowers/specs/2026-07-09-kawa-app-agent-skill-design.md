# Kawa App Agent Skill Design

## Purpose

Kawa should support not only maintainers working on the framework itself, but also application developers who use Kawa with agentic coding tools. Those agents need different guidance from framework maintainers: they should build application features through Kawa contracts and use cases, avoid leaking HTTP concerns into use cases, add tests at the right level, and verify OpenAPI/catalog output when it matters.

This work will add public, repo-distributed agent guidance for Kawa application development.

## Goals

- Provide a public Codex skill for agents that build applications with Kawa.
- Provide a short human-readable guide that explains how to use Kawa with agentic coding tools.
- Keep user-application guidance separate from Kawa framework maintainer guidance.
- Prefer current implemented Kawa behavior over future design ideas.
- Make the guidance useful for C# first, while acknowledging F# and VB.NET boundary patterns.

## Non-Goals

- Do not create a `dotnet new` template in this change.
- Do not change Kawa runtime behavior.
- Do not add new Kawa APIs.
- Do not make the app-developer skill depend on user-specific absolute paths.
- Do not include Kawa framework release, package publishing, or maintainer-only workflow details in the app-developer skill.

## Proposed Files

Create a public skill distributed from the repository:

- `skills/kawa-app-developer/SKILL.md`
- `skills/kawa-app-developer/agents/openai.yaml`
- `skills/kawa-app-developer/references/app-structure.md`
- `skills/kawa-app-developer/references/usecase-patterns.md`
- `skills/kawa-app-developer/references/testing.md`

Create a human-readable guide:

- `docs/agent-development.md`
- `docs/agent-development.ja.md`

Update discoverability:

- `README.md`

## Skill Audience

The `kawa-app-developer` skill is for agents working in applications that consume Kawa. It is not for agents changing the Kawa framework repository itself.

The skill should trigger for tasks such as:

- Creating a new Kawa use case.
- Adding a Kawa endpoint to an ASP.NET Core host.
- Adding validation or predictable failures through `KawaResult<T>` and `KawaError`.
- Writing tests for Kawa use cases or Kawa web endpoints.
- Checking OpenAPI or `/kawa/catalog.json` output in a Kawa application.
- Keeping C#, F#, or VB.NET use case implementations behind C# friendly request/response boundaries.

The maintainer workflow remains separate. Framework changes should use maintainer-oriented guidance instead of application-developer guidance.

## Skill Shape

`SKILL.md` should stay short and procedural. It should tell agents to inspect the target application first, then choose a workflow.

Core workflow:

1. Confirm the project is a Kawa application.
2. Inspect existing conventions before adding files.
3. Start new behavior from request/response contracts and `IUseCase<TRequest,TResponse>`, not from the HTTP endpoint.
4. Return `KawaResult<TResponse>` from application flow.
5. Represent predictable failures with `KawaError` and `KawaErrorKind`.
6. Map HTTP entry points with `MapKawaPost<TUseCase>` when the use case type carries one contract, or `MapKawaPost<TRequest,TResponse>` when contracts are explicit.
7. Add focused use case tests before or alongside endpoint tests.
8. Verify OpenAPI/catalog output when the change affects public API shape.

The skill should avoid embedding all examples in `SKILL.md`. Detailed examples should live in one-level reference files.

## References

`references/app-structure.md` should describe recommended application layout:

- Small C# apps may keep nested `Request` and `Response` records in the use case file.
- Larger apps may split `Contracts/`, `UseCases/`, and `Endpoints/Web/`.
- Multi-language apps should prefer C# friendly boundary contracts and language-specific use case implementation projects.

`references/usecase-patterns.md` should include concise examples for:

- C# use case with nested `Request` and `Response`.
- Validation failure with `KawaErrorKind.Validation`.
- Use case metadata through `KawaUseCaseAttribute`.
- Expected error metadata through `KawaErrorResponseAttribute`.
- Explicit request/response mapping for cross-language use cases.

`references/testing.md` should include guidance for:

- Unit testing use case success and failure paths.
- Endpoint tests for HTTP mapping only when the application has web-specific behavior to verify.
- OpenAPI/catalog checks when contract shape or metadata changes.
- Running the narrowest useful `dotnet test` command first.

## Agent Development Guide

`docs/agent-development.md` and `docs/agent-development.ja.md` should explain:

- Why Kawa is friendly to agentic development.
- Which public skill to install or copy.
- What agents should read before changing a Kawa application.
- How humans should phrase requests to agents.
- What review checklist humans should use for agent-generated Kawa application changes.

The guide should link to:

- `skills/kawa-app-developer/`
- `docs/specification.md`
- `docs/rails-like-conventions.md`
- relevant samples

If `docs/specification.md` does not exist yet when this work is implemented, the guide should link to `README.md` and the existing convention documents, then be updated after the specification documents land.

## Validation

Validation should include:

- Check the skill has valid YAML frontmatter with `name: kawa-app-developer`.
- Check `agents/openai.yaml` has a default prompt mentioning `$kawa-app-developer`.
- Search the skill and guide for user-specific absolute paths.
- Search for draft markers such as incomplete notes or template remnants.
- Verify README links resolve to existing files.
- Run a lightweight repository validation command if feasible.

## Open Questions

- Whether the repository should later add an installer script for copying repo-distributed skills into `~/.codex/skills`.
- Whether Kawa should later provide `dotnet new` templates for agent-friendly app scaffolding.
- Whether the public skill should eventually live in a separate package or marketplace channel.
