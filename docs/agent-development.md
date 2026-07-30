# Agentic Development With Kawa

Kawa is designed to be friendly to agentic coding tools because application behavior has predictable boundaries: contracts describe input and output, use cases own application flow, and transports adapt use cases to HTTP or future entry points.

This guide explains how to use the public Kawa app-development skill and how to review agent-generated Kawa application changes.

## Public Skill

The repository includes a public Codex skill for agents that build applications with Kawa:

- [`skills/kawa-app-developer/`](../skills/kawa-app-developer/)

Install or copy that skill into the agent environment that supports Codex skills. For local Codex usage, the target is usually:

```text
~/.codex/skills/kawa-app-developer/
```

The skill is for applications that consume Kawa. It is not for changing Kawa itself. Framework maintenance should use the maintainer-oriented skill instead.

## What Agents Should Read

Before changing a Kawa application, an agent should inspect the application and read:

- [Specification](specification.md)
- [Rails-like Convention Proposal](rails-like-conventions.md)
- the app's existing `Program.cs`, endpoint files, use case files, and tests
- the relevant `skills/kawa-app-developer/references/` file

For public API changes, the agent should also verify OpenAPI or `/kawa/catalog.json` when those endpoints are mapped by the application.

## How To Ask An Agent

Good requests name the use case and expected contract shape:

```text
Use $kawa-app-developer to add a CreateUser Kawa use case with request fields name and email, a validation failure when email is missing, a POST /users endpoint, and focused tests.
```

For existing applications, include the expected area or folder:

```text
Use $kawa-app-developer to add a Billing/CreateInvoice use case following the existing Contracts, UseCases, and Endpoints/Web layout.
```

For cross-language applications, name the boundary explicitly:

```text
Use $kawa-app-developer to wire the F# CreateUserUseCase through explicit C# friendly request and response contract types.
```

## Human Review Checklist

When reviewing agent-generated Kawa application changes, check:

- The change starts from request/response contracts and use cases, not from endpoint logic.
- Use cases return `KawaResult<TResponse>`.
- Predictable failures use `KawaError` and `KawaErrorKind`.
- Use cases do not depend on ASP.NET Core, `HttpContext`, HTTP status codes, CLI parser types, Worker SDK types, or RPC context types.
- Endpoints stay thin and use `MapKawaPost`.
- Public use cases include useful `KawaUseCaseAttribute` and `KawaErrorResponseAttribute` metadata when API documentation matters.
- Tests cover use case success and failure behavior.
- OpenAPI or `/kawa/catalog.json` is checked when contract shape or metadata changes.

## Related References

- [Specification](specification.md)
- [Rails-like Convention Proposal](rails-like-conventions.md)
- [C# sample](../samples/Kawa.Sample.CSharp/)
- [F# sample](../samples/Kawa.Sample.FSharp/)
- [VB.NET sample](../samples/Kawa.Sample.VB/)

