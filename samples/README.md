# Kawa 0.5 development samples

Build with .NET 10. Main development remains on net10.0; .NET 11 is tested separately for compatibility.

| Project | Purpose | Default address |
| --- | --- | --- |
| Kawa.Sample.Cli | JSON stdout, stderr errors and exit codes | local command |
| Kawa.Sample.MagicOnion.Server / Client | Unary, three streaming directions, typed hub and lobby broadcast; HTTP shares the Send use case | gRPC 5011, HTTP 5010 |
| Kawa.Sample.Grpc.Server / Client | Generated protobuf for all four RPC modes, HTTP, explicit adapters for an existing legacy.proto | gRPC 5021, HTTP 5020 |
| Kawa.Sample.Aspire.AppHost | MagicOnion + protobuf services, two discovered CLI jobs, shared ServiceDefaults | allocated by Aspire |

```sh
dotnet run --project samples/Kawa.Sample.Cli -- --message hello
# Start a server, then run its corresponding client in another terminal:
dotnet run --project samples/Kawa.Sample.MagicOnion.Server
dotnet run --project samples/Kawa.Sample.MagicOnion.Client
dotnet run --project samples/Kawa.Sample.Grpc.Server
dotnet run --project samples/Kawa.Sample.Grpc.Client
# Or let Aspire orchestrate the servers and CLI jobs:
dotnet run --project samples/Kawa.Sample.Aspire.AppHost
```

These are local unauthenticated service examples, not a production security configuration. Aspire's Dashboard retains browser-token authentication and prints its URL/token at startup. Its default https launch profile requires the ASP.NET Core development certificate; an explicit `--launch-profile http` profile is provided for localhost-only use without TLS. The AppHost uses the SDK's default NuGet-backed Dashboard/DCP setup. SDK 13.6.5 may emit ASPIRE010 because the optional CLI bundle is not enabled; the example only hosts .NET resources and does not suppress the diagnostic. Install/configure the Aspire CLI bundle if you need its additional resource types.

Protobuf generation writes only under the contracts project's obj directory. After building, export a schema explicitly:

```sh
dotnet run --project src/Kawa.Tool -- grpc export --schema samples/Kawa.Sample.Grpc.Contracts/obj/Debug/net10.0/Kawa/kawa.generated.proto --output artifacts/protos/chat.proto
```

`--force` is required to replace an existing export. The legacy.proto sample's request/response conversions are explicitly implemented in LegacyChatService.cs, rather than inferred from member names.

Template payloads are generated deterministically from these sources by `python3 eng/sync-templates.py`. `--check` detects drift. Template smoke tests use a private template hive and private package cache, install the actual Kawa.Templates package, generate each short name, restore/build it with local Kawa packages and cached external dependencies, invoke the CLI, and install/test the packaged export tool. They never install templates or tools into the user's normal profile. Online vulnerability auditing is performed by the main CI restore; the isolated offline smoke restore is a functional check.
