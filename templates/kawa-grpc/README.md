# KawaStarter (grpc)

Requires .NET 10 SDK. Restore and build before running:

```sh
dotnet restore
dotnet build --no-restore
dotnet run --project KawaStarter.Server
# In another terminal:
dotnet run --project KawaStarter.Client
```

Kawa package version defaults to 0.5.0; `--KawaVersion` can select another version during generation.

The client demonstrates unary, server streaming, client streaming and duplex calls.
The same Send use case is available over HTTP at `http://localhost:5020/messages`.
Use a JSON string for MagicOnion's HTTP request, or `{"message":"hello"}` for protobuf's HTTP request.
These localhost examples are unauthenticated; configure authentication before exposing them externally.

`Server/Protos/legacy.proto` demonstrates an existing schema with explicit request/response adapters.
Generated Kawa schemas stay under `Contracts/obj/`. To export one explicitly after building:

```sh
dotnet tool install --global Kawa.Tool --version 0.5.0
kawa grpc export --schema KawaStarter.Contracts/obj/Debug/net10.0/Kawa/kawa.generated.proto --output Protos/chat.proto
```

The export command refuses to overwrite an existing file unless `--force` is supplied.
