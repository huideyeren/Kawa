# Changelog

All notable changes to Kawa are documented here.

## [Unreleased]

## [0.5.0] - 2026-11-25

### Added

- Transport-neutral server/client/duplex streaming and typed hub contracts, executors, and payload-free activity/meter instrumentation.
- MagicOnion service/hub generation, all four raw RPC modes, typed receivers/groups, explicit generator diagnostics and typed client factory registration.
- Protobuf schema generation under `obj/`, generated server adapters, semantic DTO graph discovery and bytes/enum/nested/repeated conversions; explicit adapters for existing proto contracts.
- `Kawa.Cli` command binding with per-invocation scopes, JSON stdout, diagnostic stderr, cancellation, configurable exit codes and text formatting.
- Aspire service defaults, health endpoints, service discovery and AppHost project/reference helpers.
- Four package-backed `dotnet new` templates, runnable transport/Aspire samples, the `kawa grpc export` tool and isolated package/template smoke checks.
- A separate .NET 11 SDK/runtime compatibility CI job; main development and the primary CI job remain on .NET 10.

### Changed

- Add optional catalog transport metadata while retaining positional constructors and existing JSON fields; merge explicit non-HTTP bindings with mapped HTTP endpoints.
- Align new HTTP error kinds with OpenAPI and preserve existing numeric error values, including `Unknown = 5`.

### Fixed

- Resolve generated RPC/hub handlers by implementation identity when operations share DTOs, preserving ordinary DI override behavior.
- Terminate and dispose Core streams after the first failure.
- Parse complete protobuf DTOs with resolved types; diagnose unnumbered, duplicate, reserved and unsupported fields instead of dropping data.
- Prevent generated-name collisions across namespaces and observe Hub cancellation before operations and after asynchronous completion.
- Include MapGroup prefixes in catalog routes and start/stop the Aspire CLI sample host.
- Require matching repository/package versions and dated bilingual release notes before publishing packages.

### Security

- Require MessagePack 3.1.9 for MagicOnion and patched Aspire.Hosting 13.6.5 (whose dependency floor includes MessagePack 2.5.307).
- Validate release tags through environment variables to prevent shell injection.
- Redact Unknown/undefined error details in HTTP, RPC status/trailers and CLI output.

## [0.4.0] - 2026-07-31

### Added

- Added public Kawa specification documentation in English and Japanese.
- Added internal design documentation in English and Japanese for maintainers and adapter authors.
- Added public agent-development guidance for building Kawa applications with coding agents.
- Added distributable Codex skills for Kawa maintainers and Kawa application developers.

### Changed

- Linked the new specification and agent-development documents from the README.
- Updated GitHub Actions `actions/setup-dotnet` to v6.
- Updated Kawa Web, Swagger UI, ReDoc, ASP.NET Core test host, and test SDK dependencies.

### Security

- Added an explicit `Microsoft.OpenApi` 2.7.5 dependency to resolve the NuGet audit finding for
  `GHSA-v5pm-xwqc-g5wc`.

## [0.3.1] - 2026-06-23

### Fixed

- Prevented nested `Request` and `Response` contract types from sharing OpenAPI schema references,
  so generated clients receive the schema mapped to each use case.
- Added contract type and property XML documentation summaries to OpenAPI schema descriptions when
  the contract assembly emits an XML documentation file.

## [0.3.0] - 2026-05-28

### Changed

- Added a Kawa API catalog document endpoint that converts mapped use case contracts into a
  transport-independent catalog at `/kawa/catalog.json`.
- Renamed the F# sample from `Kawa.Sample.Mixed` to `Kawa.Sample.FSharp`.
- Updated F# and VB.NET samples to follow the one use case per file convention.
- OpenAPI metadata now describes unknown Kawa failures as `ProblemDetails` responses with
  `application/problem+json`, matching the HTTP result mapper.
- Updated test and coverage dependencies:
  - `coverlet.collector` to `10.0.1`
  - `Microsoft.NET.Test.Sdk` to `18.5.1`
  - `xunit` to `2.9.3`
  - `xunit.runner.visualstudio` to `3.1.5`
  - `Microsoft.AspNetCore.TestHost` to `10.0.8`
- Updated GitHub Actions used by CI and release workflows.

### Infrastructure

- Fixed Dependabot PR coverage uploads by ensuring Codecov receives the correct token.
- Added direct API catalog conversion tests to keep coverage checks aligned with the 0.3.0 catalog work.

## [0.2.1] - 2026-05-27

### Added

- Added OpenAPI JSON endpoint support for Kawa web applications.
- Added Swagger UI and ReDoc mapping helpers for development-time API documentation.
- Added OpenAPI operation metadata generated from use case attributes and error response metadata.

## [0.2.0] - 2026-05-26

### Added

- Added use case metadata attributes and catalog support.
- Added error response metadata for transport-independent API description.
- Added transport mapper abstractions and HTTP mapper implementations.
- Added rails-like convention proposal documentation in English and Japanese.

### Changed

- Updated samples to keep request and response contracts nested with their use case.
- Expanded design principle documentation around contracts, catalogs, and web integration.

## [0.1.1] - 2026-05-25

### Added

- Added VB.NET boundary sample and language boundary tests.
- Added NuGet release workflow and local packaging script.

[0.4.0]: https://github.com/huideyeren/Kawa/compare/v0.3.1...v0.4.0
[0.3.1]: https://github.com/huideyeren/Kawa/compare/v0.3.0...v0.3.1
[0.3.0]: https://github.com/huideyeren/Kawa/compare/v0.2.1...v0.3.0
[0.2.1]: https://github.com/huideyeren/Kawa/compare/v0.2.0...v0.2.1
[0.2.0]: https://github.com/huideyeren/Kawa/compare/v0.1.1...v0.2.0
[0.1.1]: https://github.com/huideyeren/Kawa/releases/tag/v0.1.1
