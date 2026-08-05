# Kawa 0.5.0 Transport Adapters Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Deliver Kawa 0.5.0 as a backward-compatible release with MagicOnion raw gRPC and StreamingHub, standard protobuf gRPC in both Kawa-originated and existing-proto directions, `System.CommandLine` CLI I/O, Aspire integration, templates, samples, tests, and bilingual documentation.

**Architecture:** Keep application contracts and executors in `Kawa.Abstractions`/`Kawa.Core`; ship MagicOnion, protobuf gRPC, CLI, and Aspire as independent adapters. An incremental generator emits MagicOnion contracts, while an MSBuild task emits Kawa-originated protobuf schemas before `Grpc.Tools` runs. Existing protobuf applications use explicit adapters rather than inferred member mapping. New transports add catalog metadata but do not change current HTTP behavior.

**Tech Stack:** .NET 10, MagicOnion.Server 7.10.2, System.CommandLine 2.0.10, .NET Aspire 13.4.6, xUnit, Roslyn incremental generators, ASP.NET Core TestHost.

## Global Constraints

- Target `net10.0`; preserve Kawa 0.4 public behavior.
- `Kawa.Abstractions` and `Kawa.Core` must not reference ASP.NET Core, MagicOnion, System.CommandLine, or Aspire.
- Add the six approved `KawaErrorKind` values exactly: `Cancelled`, `DeadlineExceeded`, `AlreadyExists`, `PreconditionFailed`, `ResourceExhausted`, `Unavailable`.
- Stream failures terminate transports; they are never emitted as successful stream payloads.
- Generated contracts use attributes `KawaMagicOnionServiceAttribute`, `KawaMagicOnionOperationAttribute`, and `KawaMagicOnionHubAttribute`; generated namespace suffix is `.Generated`.
- Protobuf schemas generated from Kawa contracts require stable `[KawaProtoField(n)]` numbers; `Kawa.Grpc.ProtoAdapter` only provides explicit `IGrpcProtoAdapter<TProto,TContract>` registration.
- Keep English/Japanese specification, internal design, and changelog section order aligned.
- Every implementation task starts with a failing test, ends with its narrow test command, and commits only its own files.

---

## File Structure

- `src/Kawa.Abstractions/`: stream/hub interfaces, error enum, and transport catalog records.
- `src/Kawa.Core/`: executors for stream and hub contracts.
- `src/Kawa.MagicOnion/`: runtime registration, gRPC error mapper, generated-service mapping helpers, and attributes.
- `src/Kawa.MagicOnion.Generators/`: Roslyn analyzer package and generated source.
- `src/Kawa.Grpc/`, `src/Kawa.Grpc.Generators/`, and `src/Kawa.Grpc.ProtoAdapter/`: standard gRPC runtime, Kawa-first protobuf generation, and explicit existing-proto adapters.
- `src/Kawa.Cli/`: System.CommandLine binding, JSON/text writers, and exit-code mapper.
- `src/Kawa.Aspire/` and `src/Kawa.Aspire.Hosting/`: service defaults and AppHost extensions.
- `tests/Kawa.*.Tests/`: one focused project per new package plus existing regression suites.
- `templates/`: three `dotnet new` templates; `samples/`: their runnable counterparts.

### Task 1: Add transport-independent errors, stream contracts, and hub session contracts

**Files:**
- Create: `src/Kawa.Abstractions/IServerStreamingUseCase.cs`, `IClientStreamingUseCase.cs`, `IDuplexStreamingUseCase.cs`, `IStreamingHubUseCase.cs`, `IKawaHubSession.cs`
- Modify: `src/Kawa.Abstractions/KawaErrorKind.cs`
- Test: `tests/Kawa.Core.Tests/StreamContractTests.cs`

**Produces:**
```csharp
IAsyncEnumerable<KawaResult<TResponse>> IServerStreamingUseCase<TRequest,TResponse>.ExecuteAsync(TRequest request, CancellationToken cancellationToken = default);
Task<KawaResult<TResponse>> IClientStreamingUseCase<TRequest,TResponse>.ExecuteAsync(IAsyncEnumerable<TRequest> requests, CancellationToken cancellationToken = default);
IAsyncEnumerable<KawaResult<TResponse>> IDuplexStreamingUseCase<TRequest,TResponse>.ExecuteAsync(IAsyncEnumerable<TRequest> requests, CancellationToken cancellationToken = default);
Task IStreamingHubUseCase<TClientEvent,TServerEvent>.OnConnectedAsync(IKawaHubSession<TServerEvent> session, CancellationToken cancellationToken = default);
Task IStreamingHubUseCase<TClientEvent,TServerEvent>.HandleAsync(TClientEvent clientEvent, IKawaHubSession<TServerEvent> session, CancellationToken cancellationToken = default);
Task IStreamingHubUseCase<TClientEvent,TServerEvent>.OnDisconnectedAsync(IKawaHubSession<TServerEvent> session, Exception? exception, CancellationToken cancellationToken = default);
```

- [ ] **Step 1: Write failing enum and contract-shape tests**
```csharp
[Fact]
public void ErrorKind_ContainsTransportNeutralValues()
{
    Assert.Equal("Unavailable", KawaErrorKind.Unavailable.ToString());
    Assert.Equal("ResourceExhausted", KawaErrorKind.ResourceExhausted.ToString());
}
```
- [ ] **Step 2: Run `dotnet test tests/Kawa.Core.Tests/Kawa.Core.Tests.csproj --no-restore --filter StreamContractTests` and confirm compilation fails.**
- [ ] **Step 3: Add the exact interfaces and enum members, with XML documentation explaining transport independence.**
- [ ] **Step 4: Re-run the command; expect PASS.**
- [ ] **Step 5: Commit `feat: add Kawa streaming and hub contracts`.**

### Task 2: Add Core executors and extend HTTP error mapping

**Files:**
- Create: `src/Kawa.Core/ServerStreamingUseCaseExecutor.cs`, `ClientStreamingUseCaseExecutor.cs`, `DuplexStreamingUseCaseExecutor.cs`, `StreamingHubUseCaseExecutor.cs`
- Modify: `src/Kawa.Web/KawaHttpErrorMapper.cs`, `tests/Kawa.Web.Tests/KawaHttpResultConverterTests.cs`
- Test: `tests/Kawa.Core.Tests/StreamUseCaseExecutorTests.cs`

**Consumes:** Task 1 contracts. **Produces:** executor `ExecuteAsync` methods matching their corresponding interfaces and HTTP mappings 499/504/409/412/429/503.

- [ ] **Step 1: Write tests proving each executor forwards cancellation and returns a stream/result unchanged; add six HTTP status theory cases.**
```csharp
await foreach (var item in executor.ExecuteAsync(useCase, request, cancellationToken))
    Assert.True(item.IsSuccess);
```
- [ ] **Step 2: Run Core and Web targeted tests; expect missing executor/types and failed status cases.**
- [ ] **Step 3: Implement null-guarded thin executors; map `Cancelled` to 499, `DeadlineExceeded` to 504, `AlreadyExists` to 409, `PreconditionFailed` to 412, `ResourceExhausted` to 429, and `Unavailable` to 503.**
- [ ] **Step 4: Run `dotnet test Kawa.sln --no-restore --disable-build-servers`; expect PASS.**
- [ ] **Step 5: Commit `feat: add streaming executors and transport errors`.**

### Task 3: Create MagicOnion runtime contracts and error conversion

**Files:**
- Create: `src/Kawa.MagicOnion/Kawa.MagicOnion.csproj`, `KawaMagicOnionServiceAttribute.cs`, `KawaMagicOnionOperationAttribute.cs`, `KawaMagicOnionHubAttribute.cs`, `KawaMagicOnionErrorMapper.cs`, `KawaMagicOnionServiceCollectionExtensions.cs`, `KawaMagicOnionEndpointRouteBuilderExtensions.cs`
- Create: `tests/Kawa.MagicOnion.Tests/Kawa.MagicOnion.Tests.csproj`, `KawaMagicOnionErrorMapperTests.cs`
- Modify: `Kawa.sln`, `eng/pack.sh`

**Produces:** `AddKawaMagicOnion(IServiceCollection)` and `MapKawaMagicOnionServices(IEndpointRouteBuilder)` plus `kawa-error-bin` UTF-8 JSON trailers.

- [ ] **Step 1: Write mapper tests for every existing and new `KawaErrorKind`, asserting gRPC status and `kawa-error-bin` trailer JSON.**
```csharp
Assert.Equal(StatusCode.Unavailable, mapper.ToRpcException(error).StatusCode);
Assert.Contains("Unavailable", Encoding.UTF8.GetString(trailers.GetBytes("kawa-error-bin")));
```
- [ ] **Step 2: Run the new test project and confirm it fails before the project exists.**
- [ ] **Step 3: Add the runtime project with MagicOnion.Server 7.10.2; implement status mapping and DI/endpoint extension null guards.**
- [ ] **Step 4: Run its tests and build the solution.**
- [ ] **Step 5: Commit `feat: add MagicOnion runtime integration`.**

### Task 4: Implement the incremental MagicOnion generator

**Files:**
- Create: `src/Kawa.MagicOnion.Generators/Kawa.MagicOnion.Generators.csproj`, `KawaMagicOnionGenerator.cs`, `Diagnostics.cs`
- Create: `tests/Kawa.MagicOnion.Generators.Tests/Kawa.MagicOnion.Generators.Tests.csproj`, `GeneratorTests.cs`
- Modify: `src/Kawa.MagicOnion/Kawa.MagicOnion.csproj`, `Kawa.sln`, `eng/pack.sh`

**Produces:** generated `.Generated` unary/raw-stream service interfaces, hub/receiver interfaces, server adapters, and diagnostics `KAWA5001` through `KAWA5004` for unsupported shape, duplicate name, incomplete hub, and ambiguous use case.

- [ ] **Step 1: Add generator snapshots for one unary service, all three stream services, a hub, and each diagnostic.**
- [ ] **Step 2: Run the generator tests; expect generated-source assertions to fail.**
- [ ] **Step 3: Implement an `IIncrementalGenerator` that reads the three attributes, groups by service/hub name, and emits only deterministic fully-qualified type names.**
- [ ] **Step 4: Re-run generator tests and `dotnet build Kawa.sln --no-restore --disable-build-servers`; expect PASS.**
- [ ] **Step 5: Commit `feat: generate MagicOnion Kawa adapters`.**

### Task 5: Prove all MagicOnion communications end to end

**Files:**
- Create: `tests/Kawa.MagicOnion.Tests/MagicOnionTransportTests.cs`, `StreamingHubTests.cs`, `GeneratedTestUseCases.cs`
- Modify: `src/Kawa.MagicOnion/KawaMagicOnionEndpointRouteBuilderExtensions.cs`

**Consumes:** generated adapters from Task 4. **Produces:** verified Unary, server/client/duplex stream, typed event, lifecycle, and group behavior.

- [ ] **Step 1: Write TestServer-based tests that invoke every raw gRPC mode, assert a failed stream terminates with the mapped status/trailer, and assert two hub clients receive a group broadcast.**
- [ ] **Step 2: Run `dotnet test tests/Kawa.MagicOnion.Tests/Kawa.MagicOnion.Tests.csproj --no-restore`; expect failures.**
- [ ] **Step 3: Complete generated-service discovery/mapping and the MagicOnion-backed `IKawaHubSession<TServerEvent>` implementation.**
- [ ] **Step 4: Re-run the project and solution tests; expect PASS.**
- [ ] **Step 5: Commit `test: cover MagicOnion streaming and hubs`.**

### Task 6: Add standard protobuf gRPC generation and existing-proto adapters

**Files:**
- Create: `src/Kawa.Grpc/Kawa.Grpc.csproj`, `KawaProtoServiceAttribute.cs`, `KawaProtoFieldAttribute.cs`, `KawaGrpcErrorMapper.cs`, `KawaGrpcServiceCollectionExtensions.cs`
- Create: `src/Kawa.Grpc.Generators/Kawa.Grpc.Generators.csproj`, `KawaProtoSchemaGenerationTask.cs`, `Diagnostics.cs`
- Create: `src/Kawa.Grpc.ProtoAdapter/Kawa.Grpc.ProtoAdapter.csproj`, `IGrpcProtoAdapter.cs`, `GrpcProtoAdapterServiceCollectionExtensions.cs`
- Create: `tests/Kawa.Grpc.Tests/Kawa.Grpc.Tests.csproj`, `KawaProtoGeneratorTests.cs`, `GrpcProtoAdapterTests.cs`
- Modify: `Kawa.sln`, `eng/pack.sh`, `docs/superpowers/specs/2026-07-31-kawa-050-transports-design.md`, `docs/superpowers/specs/2026-07-31-kawa-050-transports-design.ja.md`

**Produces:** Kawa-first `.proto` schemas under `obj/`, explicit `kawa grpc export` output, standard gRPC service adapters, and an explicit existing-proto request/response adapter contract.

- [ ] **Step 1: Write failing generator tests that require `[KawaProtoField(1)]` and assert a unary `service`/`rpc` schema plus a duplicate-field-number diagnostic; write adapter tests for explicit request and response conversion.**
```csharp
Assert.Contains("service Chat", generatedProto);
Assert.Contains("rpc Send(SendRequest) returns (SendResponse)", generatedProto);
Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "KAWA5101");
```
- [ ] **Step 2: Run `dotnet test tests/Kawa.Grpc.Tests/Kawa.Grpc.Tests.csproj --no-restore`; expect compilation failures before the projects exist.**
- [ ] **Step 3: Implement protobuf attributes, the MSBuild schema-generation task that runs before `Grpc.Tools`, gRPC status/trailer conversion, and `IGrpcProtoAdapter<TProto, TContract>` with no inferred member mapping.**
- [ ] **Step 4: Add a generated protobuf server/client smoke test for unary and all three gRPC stream directions; run the gRPC project tests and `dotnet test Kawa.sln --no-restore --disable-build-servers`; expect PASS.**
- [ ] **Step 5: Commit `feat: add protobuf gRPC adapters`.**

### Task 7: Add System.CommandLine CLI integration

**Files:**
- Create: `src/Kawa.Cli/Kawa.Cli.csproj`, `KawaCommandExtensions.cs`, `KawaCliResultWriter.cs`, `IKawaCliExitCodeMapper.cs`, `KawaDefaultCliExitCodeMapper.cs`
- Create: `tests/Kawa.Cli.Tests/Kawa.Cli.Tests.csproj`, `KawaCommandExtensionsTests.cs`, `KawaCliResultWriterTests.cs`
- Modify: `Kawa.sln`, `eng/pack.sh`

**Produces:** commands that write success JSON only to stdout, failures only to stderr, and map failure to exit code 1 unless an `IKawaCliExitCodeMapper` is registered.

- [ ] **Step 1: Write invocation tests with `StringWriter` asserting `{"value":"ok"}` on stdout/0 and a validation error on stderr/1; add a custom mapper test returning 23.**
- [ ] **Step 2: Run the project tests and confirm compilation fails.**
- [ ] **Step 3: Implement `Command` extension overloads for unary use cases, JSON serialization with `JsonSerializerDefaults.Web`, and optional text formatter registration.**
- [ ] **Step 4: Re-run CLI tests and the solution suite; expect PASS.**
- [ ] **Step 5: Commit `feat: add System.CommandLine integration`.**

### Task 8: Add Aspire service and hosting integration

**Files:**
- Create: `src/Kawa.Aspire/Kawa.Aspire.csproj`, `KawaAspireServiceCollectionExtensions.cs`, `KawaAspireEndpointRouteBuilderExtensions.cs`
- Create: `src/Kawa.Aspire.Hosting/Kawa.Aspire.Hosting.csproj`, `KawaDistributedApplicationBuilderExtensions.cs`
- Create: `tests/Kawa.Aspire.Tests/Kawa.Aspire.Tests.csproj`, `KawaAspireDefaultsTests.cs`, `KawaHostingExtensionsTests.cs`
- Modify: `Kawa.sln`, `eng/pack.sh`

**Produces:** `AddKawaAspireDefaults()`, `/health` and `/alive` mapping, and AppHost extensions `AddKawaMagicOnionProject`/`AddKawaCliProject` using Aspire 13.4.6.

- [ ] **Step 1: Write tests asserting the service collection contains Kawa/MagicOnion Activity and Meter sources, health endpoints respond, and AppHost resources expose a gRPC endpoint/reference.**
- [ ] **Step 2: Run Aspire tests; expect missing project/API failures.**
- [ ] **Step 3: Implement service defaults with OpenTelemetry/service discovery and hosting extensions over `IDistributedApplicationBuilder`.**
- [ ] **Step 4: Run Aspire tests, then the full solution.**
- [ ] **Step 5: Commit `feat: add Aspire Kawa integrations`.**

### Task 9: Add templates and runnable samples

**Files:**
- Create: `templates/kawa-cli/.template.config/template.json`, `templates/kawa-magiconion/.template.config/template.json`, `templates/kawa-grpc/.template.config/template.json`, `templates/kawa-aspire/.template.config/template.json`
- Create: `samples/Kawa.Sample.Cli/`, `samples/Kawa.Sample.MagicOnion.Server/`, `samples/Kawa.Sample.MagicOnion.Client/`, `samples/Kawa.Sample.Grpc.Server/`, `samples/Kawa.Sample.Grpc.Client/`, `samples/Kawa.Sample.Aspire.AppHost/`, `samples/Kawa.Sample.Aspire.ServiceDefaults/`
- Create: `tests/Kawa.Templates.Tests/Kawa.Templates.Tests.csproj`, `TemplateSmokeTests.cs`
- Modify: `Kawa.sln`, `eng/pack.sh`

- [ ] **Step 1: Write smoke tests that install `Kawa.Templates` from a local package, instantiate each short name, restore, build, and invoke the CLI sample.**
- [ ] **Step 2: Run template tests; expect missing template/package failures.**
- [ ] **Step 3: Add the four template manifests and chat-domain samples demonstrating HTTP, all raw gRPC modes, hub groups, CLI JSON, Kawa-originated protobuf schema export, an explicit existing-proto adapter, and Aspire AppHost topology.**
- [ ] **Step 4: Run each generated project restore/build plus template tests; expect PASS.**
- [ ] **Step 5: Commit `feat: add Kawa 0.5 templates and samples`.**

### Task 10: Enrich catalog, finalize documents, release metadata, and verification

**Files:**
- Modify: `src/Kawa.Abstractions/KawaApiCatalog.cs`, `src/Kawa.Abstractions/KawaApiCatalogContract.cs`, `src/Kawa.Abstractions/KawaApiCatalogErrorResponse.cs`, `src/Kawa.Abstractions/KawaApiCatalogUseCase.cs`, `src/Kawa.Web/KawaApiCatalogEndpointRouteBuilderExtensions.cs`, `tests/Kawa.Core.Tests/KawaApiCatalogTests.cs`
- Modify: `docs/specification.md`, `docs/specification.ja.md`, `docs/internal-design.md`, `docs/internal-design.ja.md`, `CHANGELOG.md`, `CHANGELOG.ja.md`, `README.md`, `Directory.Build.props`

**Produces:** optional catalog `transports` metadata, complete bilingual docs, and version `0.5.0`.

- [ ] **Step 1: Write a catalog serialization regression test asserting existing JSON fields stay unchanged and an HTTP+gRPC entry contains `transports: [{"kind":"http"}, {"kind":"grpc"}]`.**
- [ ] **Step 2: Run the catalog test and confirm it fails.**
- [ ] **Step 3: Add optional transport records without removing existing fields; document every public contract, mapping, template, and compatibility promise in both languages; set `<Version>0.5.0</Version>`.**
- [ ] **Step 4: Run `dotnet restore Kawa.sln --disable-build-servers`, `dotnet test Kawa.sln --no-restore --disable-build-servers`, and `bash eng/pack.sh`; inspect all package names and versions.**
- [ ] **Step 5: Commit `docs: finalize Kawa 0.5.0 transport release`.**

## Plan Self-Review

- Spec coverage: Tasks 1-2 cover transport-independent contracts/errors and HTTP compatibility; Tasks 3-5 cover all MagicOnion modes and StreamingHub; Task 6 covers both Kawa-originated and existing-proto protobuf gRPC; Task 7 covers CLI; Task 8 covers both Aspire packages; Task 9 covers all templates/samples; Task 10 covers catalog, bilingual docs, changelog, version, packaging, and regression verification.
- Placeholder scan: no deferred implementation or unnamed API is present; all planned public types, package versions, test targets, and output behavior are named above.
- Type consistency: Tasks 2-5 consume the exact contracts produced by Task 1; Tasks 3-5 use the exact generator attributes and runtime extensions defined in the global constraints; Task 7 consumes the runtime package built by Tasks 3-5.
