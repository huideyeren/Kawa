## Design Principles

Kawa is a contract-first .NET web framework that lays a thin waterway on top of ASP.NET Core.

## Packages

- `Kawa.Abstractions`: shared use case, result, and error contracts
- `Kawa.Core`: transport-independent use case execution
- `Kawa.Web`: ASP.NET Core Minimal API integration
- `Kawa.FSharp`: F# helpers

```bash
dotnet add package Kawa.Web
```

To create local NuGet packages:

```bash
bash eng/pack.sh
```

To publish the generated packages to NuGet.org:

```bash
NUGET_API_KEY=... bash eng/push-nuget.sh
```

See:

- [Design Principles](docs/design-principles.md)
- [設計思想メモ 日本語版](docs/design-principles.ja.md)

## License

Kawa is licensed under the MIT License.
