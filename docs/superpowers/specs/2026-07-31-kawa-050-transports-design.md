# Kawa 0.5.0 Transport Adapters Design

## Status

Approved design. This document defines the intended 0.5.0 scope before implementation begins.

## Goals

Kawa 0.5.0 adds first-class, independently installable adapters for MagicOnion gRPC, protobuf gRPC, command-line applications, and .NET Aspire. Each adapter must let an application reuse Kawa use cases without adding transport-specific types to its application layer.

The release includes NuGet packages, `dotnet new` templates, runnable samples, English and Japanese public and maintainer documentation, and automated validation.

## Non-goals

- Change the existing HTTP behavior in `Kawa.Web`, its OpenAPI output, or its API catalog contract incompatibly.
- Add ASP.NET Core, MagicOnion, `System.CommandLine`, or Aspire dependencies to `Kawa.Abstractions` or `Kawa.Core`.
- Make every MagicOnion feature a Kawa abstraction. Interceptors, serializers, authentication middleware, and deployment remain application/MagicOnion configuration concerns.
- Add Worker, controller, or unrelated persistence integrations.

## Package Layout

| Package | Responsibility | Dependency direction |
| --- | --- | --- |
| `Kawa.MagicOnion` | Runtime server/client integration, DI registration, generated-adapter support, and Kawa error conversion to gRPC. | Depends on Core, Abstractions, and MagicOnion. |
| `Kawa.MagicOnion.Generators` | Incremental source generator and diagnostics for service contracts, adapters, and client registration. | Analyzer-only dependency; generated code depends on `Kawa.MagicOnion`. |
| `Kawa.Grpc` | Standard protobuf gRPC runtime integration, Kawa-originated `.proto` generation, and gRPC status/trailer conversion. | Depends on Core, Abstractions, ASP.NET Core gRPC, and protobuf tooling. |
| `Kawa.Grpc.Generators` | An MSBuild schema-generation task that emits `.proto` schemas before `Grpc.Tools` compiles them. | Build-time dependency; generated code depends on `Kawa.Grpc`. |
| `Kawa.Grpc.ProtoAdapter` | Explicitly connects existing generated protobuf message types and service bases to Kawa use cases. | Depends on Core, Abstractions, and gRPC generated types. |
| `Kawa.Cli` | `System.CommandLine` command binding, standard I/O, JSON output, diagnostics, and exit-code mapping. | Depends on Core and Abstractions. |
| `Kawa.Aspire` | Service-side Kawa observability, health checks, and service-discovery integration. | Depends on the relevant Aspire client/telemetry packages. |
| `Kawa.Aspire.Hosting` | AppHost project-resource extensions for Kawa MagicOnion and CLI projects. | Depends on Aspire Hosting only. |
| `Kawa.Templates` | `dotnet new` templates for CLI, MagicOnion, and Aspire distributed applications. | Template package; no runtime dependency. |

`Kawa.Abstractions` remains transport-independent. `Kawa.Core` remains transport-independent and does not reference an adapter package. `Kawa.Web` remains the HTTP adapter and is not made a dependency of any new adapter.

## Transport-Independent Contracts

The existing `IUseCase<TRequest, TResponse>` remains the unary operation contract.

The following public stream contracts are added to `Kawa.Abstractions`:

- `IServerStreamingUseCase<TRequest, TResponse>`: accepts one request and produces `IAsyncEnumerable<KawaResult<TResponse>>`.
- `IClientStreamingUseCase<TRequest, TResponse>`: accepts `IAsyncEnumerable<TRequest>` and produces `Task<KawaResult<TResponse>>`.
- `IDuplexStreamingUseCase<TRequest, TResponse>`: accepts `IAsyncEnumerable<TRequest>` and produces `IAsyncEnumerable<KawaResult<TResponse>>`.

`Kawa.Core` adds deliberately thin executors for each contract. The executors own cancellation propagation and null validation only; they do not acquire transport concerns.

For a server or duplex output stream, a failed `KawaResult<TResponse>` terminates the transport with the adapter's error form. It is not serialized as a success message in the stream. This prevents clients from treating an error as domain data.

## StreamingHub Contract

StreamingHub is supported as a separate, typed event-and-session abstraction rather than being treated as ordinary duplex streaming.

`Kawa.Abstractions` adds:

- `IStreamingHubUseCase<TClientEvent, TServerEvent>` for connection lifecycle and client-event handling.
- `IKawaHubSession<TServerEvent>` for sending typed server events, broadcasting, joining/leaving named groups, and reading connection identity.
- lifecycle contracts for connection and disconnection, so applications can release state even when a transport closes unexpectedly.

The contracts expose neither MagicOnion service-context types nor gRPC stream readers/writers. `Kawa.MagicOnion` supplies the `IKawaHubSession<TServerEvent>` implementation using MagicOnion's typed receiver and group APIs.

## MagicOnion Integration and Generation

An application opts in by applying `[KawaMagicOnionService("service-name")]` to a unary or raw-stream use-case class, and `[KawaMagicOnionHub("hub-name")]` to an `IStreamingHubUseCase<TClientEvent, TServerEvent>` implementation. `[KawaMagicOnionOperation("operation-name")]` optionally overrides the generated operation name; otherwise the use-case class name without a `UseCase` suffix is used. Generated public types are emitted into the annotated type's namespace with a `.Generated` suffix. The generator must provide these outcomes:

1. A shared MagicOnion service interface for unary, server-streaming, client-streaming, and duplex operations.
2. A typed `IStreamingHub` and receiver interface for each declared Kawa hub.
3. Server adapter classes that resolve the matching Kawa use case and executor from DI.
4. Client-registration helpers that use the generated shared contract, rather than reflection or runtime code generation.
5. Diagnostics for unsupported serializable shapes, duplicate service/operation/event names, incomplete hub declarations, and ambiguous use-case contracts.

The generator must be incremental, deterministic, and usable by both client and server projects. Contract assemblies may be shared without referencing an ASP.NET Core host.

The runtime API is intentionally small: applications register Kawa and MagicOnion services through `AddKawaMagicOnion()` and map the generated services through `MapKawaMagicOnionServices()`. MagicOnion-specific middleware, serialization settings, authentication, and interceptors remain directly configurable by the application.

Raw gRPC supports all four communication patterns. StreamingHub support includes typed client receivers, connection lifecycle, direct server events, group broadcasts, and named group membership. It does not promise to abstract every vendor-specific MagicOnion extension.

## Standard Protobuf gRPC Integration

`Kawa.Grpc` is the Kawa-first standard gRPC adapter. Kawa use cases annotated for protobuf generation produce a deterministic `.proto` schema under the build output directory, standard gRPC service adapters, and client helpers. An MSBuild schema-generation task runs before `Grpc.Tools`, which then consumes the generated schema during the same build. This is deliberately not a Roslyn source generator: the `.proto` must exist before protobuf compilation starts. The `kawa grpc export` command explicitly copies the same schema into an application's `Protos/` directory for source control and distribution to non-.NET clients; ordinary builds never modify tracked source files.

Kawa-originated protobuf contracts support scalar values, `string`, `bool`, `byte[]`, enums, nested contracts, and repeated values. Every serialized member requires `[KawaProtoField(n)]`; changing or reusing a field number is a breaking protocol change and produces a diagnostic.

`Kawa.Grpc.ProtoAdapter` supports existing `.proto` contracts without making them a Kawa dependency. Applications implement `IGrpcProtoAdapter<TProto, TContract>` for request and response conversion, then register a generated-service-base adapter explicitly. The adapter deliberately performs no property-name inference: protobuf schema ownership, message conversion, and compatibility remain the application's responsibility.

Standard gRPC uses the same `KawaError` status mapping and UTF-8 JSON `kawa-error-bin` trailer as MagicOnion. The three raw streaming patterns map to standard gRPC streaming APIs. StreamingHub remains MagicOnion-specific and has no protobuf gRPC counterpart.

## Error Model

Existing `KawaErrorKind` values retain their meaning. The following transport-neutral kinds are added:

| New kind | Meaning | gRPC status | HTTP mapping |
| --- | --- | --- | --- |
| `Cancelled` | The caller cancelled the operation. | `Cancelled` | 499 Client Closed Request; the mapping is not emitted when the HTTP connection is already closed. |
| `DeadlineExceeded` | The operation did not finish by its deadline. | `DeadlineExceeded` | 504 Gateway Timeout |
| `AlreadyExists` | The attempted creation target already exists. | `AlreadyExists` | 409 Conflict |
| `PreconditionFailed` | Current state does not meet the operation's stated precondition. | `FailedPrecondition` | 412 Precondition Failed |
| `ResourceExhausted` | A quota, capacity, or rate limit was exhausted. | `ResourceExhausted` | 429 Too Many Requests |
| `Unavailable` | A temporary dependency or service failure makes retry meaningful. | `Unavailable` | 503 Service Unavailable |

`Kawa.MagicOnion` maps existing kinds as follows: `Validation` to `InvalidArgument`, `Unauthorized` to `Unauthenticated`, `Forbidden` to `PermissionDenied`, `NotFound` to `NotFound`, `Conflict` to `Aborted`, and `Unknown` to `Internal`.

MagicOnion transports preserve a UTF-8 JSON serialized `KawaError` in the binary `kawa-error-bin` gRPC trailer for predictable errors. Stream failures end the RPC with that status and trailer. Hub operation failures surface as the corresponding RPC failure; connection-level failures close the affected connection with the same mapped status.

## CLI Integration

`Kawa.Cli` standardizes execution around `System.CommandLine`. It provides command-binding and invocation helpers, not a replacement argument parser.

- Successful structured results are written only to standard output as JSON by default.
- Predictable errors and unexpected exceptions are written only to standard error.
- The default exit code is `0` for success and `1` for every failure.
- Applications can register `IKawaCliExitCodeMapper` for an application-specific exit-code policy.
- Applications can register a text formatter for human-oriented output; it does not change the default JSON contract.
- Cancellation tokens originate from command invocation and console cancellation, then flow through Kawa executors.

## Aspire Integration

`Kawa.Aspire` is a service-side integration. Its `AddKawaAspireDefaults()` extension adds Kawa/MagicOnion activity and meter sources to OpenTelemetry, registers `/health` and `/alive` health-check endpoints for ASP.NET Core hosts, and enables service discovery required by generated MagicOnion clients.

`Kawa.Aspire.Hosting` is AppHost-only. It provides project-resource extensions for:

- adding a MagicOnion service with its gRPC endpoint and service-discovery information;
- adding a CLI project as an executable resource, including arguments, environment values, and explicit dependencies;
- referencing Kawa MagicOnion services from consumer projects.

The hosting package does not run services itself and does not force Aspire onto applications that use only Kawa.Web, MagicOnion, or CLI.

## Catalog and Documentation

The current HTTP catalog stays compatible. Each entry gains an optional, additive `transports` array. Its values identify `http`, `grpc`, `cli`, or `streamingHub` exposure and include the respective route, service/operation, command path, or hub name. A gRPC entry identifies whether it uses `magicOnion` or `protobuf`. Consumers that do not read `transports` retain current behavior.

The implementation updates both language versions of:

- `docs/specification.*` for public packages, contracts, transport mappings, generated contracts, templates, and compatibility;
- `docs/internal-design.*` for dependency direction, generator design, catalog enrichment, telemetry, and test strategy;
- `CHANGELOG.*` for the 0.5.0 release notes.

## Templates and Samples

`Kawa.Templates` supplies three templates:

1. A CLI application with `System.CommandLine`, JSON output, a formatted-output extension point, and error/exit-code behavior.
2. A MagicOnion server/client pair showing unary, all three raw stream directions, and a typed StreamingHub.
3. A protobuf gRPC server/client pair showing Kawa-originated schema export and an existing-proto adapter.
4. An Aspire distributed application containing AppHost, ServiceDefaults, a MagicOnion service, a protobuf gRPC service, and a CLI client/job resource.

Runnable samples mirror the templates and share a small chat-oriented domain. The samples demonstrate the same application contracts through HTTP, MagicOnion's four raw gRPC modes, StreamingHub events/groups, and CLI commands. The Aspire sample starts the topology and exposes telemetry in the dashboard.

## Verification Strategy

- Core tests cover all new stream executors, cancellation, and terminal-result behavior.
- MagicOnion integration tests cover unary, server/client/duplex streaming, generated contract usability, status/trailer conversion, hub connect/disconnect, event delivery, and group broadcast.
- Protobuf gRPC tests cover generated schemas, `Grpc.Tools` compilation, unary and all raw stream directions, status/trailer conversion, schema field-number diagnostics, and explicit existing-proto adapters.
- Generator tests assert generated sources and all diagnostics.
- CLI tests cover parsing integration, standard output/error separation, JSON and custom formatting, cancellation, and exit-code substitution.
- Aspire tests cover AppHost resource references, service-discovery configuration, health checks, and emitted telemetry.
- Template tests instantiate each template, restore/build it, and run the appropriate smoke test.
- Existing Core and Web tests continue to run unchanged to demonstrate 0.4 compatibility.

## Compatibility and Release

0.5.0 is a backward-compatible minor release: it adds packages, contracts, catalog fields, and error kinds without removing or changing existing public behavior. The implementation must version all published packages consistently at `0.5.0` and package the new projects through the existing release workflow.

## Implementation Order

1. Add transport-neutral streaming, hub, and error contracts with Core executors and tests.
2. Add the MagicOnion runtime package, source generator, generated-code tests, and gRPC/hub integration tests.
3. Add protobuf gRPC runtime, Kawa-originated schema generator, explicit proto adapter, and interoperability tests.
4. Add CLI integration, template, sample, and tests.
5. Add Aspire service and hosting packages, distributed template/sample, and AppHost tests.
6. Enrich the catalog, update English/Japanese documentation and changelogs, validate all templates, and package the full solution.
