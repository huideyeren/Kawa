# KawaStarter (magiconion)

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
The same Send use case is available over HTTP at `http://localhost:5010/messages`.
Use a JSON string for MagicOnion's HTTP request, or `{"message":"hello"}` for protobuf's HTTP request.
These localhost examples are unauthenticated; configure authentication before exposing them externally.
The client also connects two typed StreamingHub receivers to the lobby and verifies group broadcast.
