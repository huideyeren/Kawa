# Changelog

All notable changes to Kawa are documented here.

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

[0.3.0]: https://github.com/huideyeren/Kawa/compare/v0.2.1...v0.3.0
[0.2.1]: https://github.com/huideyeren/Kawa/compare/v0.2.0...v0.2.1
[0.2.0]: https://github.com/huideyeren/Kawa/compare/v0.1.1...v0.2.0
[0.1.1]: https://github.com/huideyeren/Kawa/releases/tag/v0.1.1
