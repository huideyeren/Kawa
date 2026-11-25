# KawaStarter (aspire)

Requires .NET 10 SDK. Restore and build before running:

```sh
dotnet restore
dotnet build --no-restore
dotnet run --project KawaStarter.AppHost
```

Kawa package version defaults to 0.5.0; `--KawaVersion` can select another version during generation.

The AppHost orchestrates MagicOnion and protobuf gRPC servers and two CLI jobs, using named HTTP/2 endpoints and service discovery. ServiceDefaults configures telemetry and health checks. The Dashboard URL/token is printed by Aspire at startup. The default https launch profile requires the ASP.NET Core development certificate; `--launch-profile http` explicitly selects localhost-only HTTP. Install Aspire's local prerequisites before running the AppHost; building does not launch its resources. Hosting dependencies and the AppHost SDK require patched Aspire 13.6.5 or later. The local example uses plaintext HTTP/2; configure TLS/authentication for deployment.
