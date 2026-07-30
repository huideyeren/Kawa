# Kawa におけるエージェント開発

Kawa は、エージェントによるコーディングと相性がよいように設計されています。入力と出力は contract が表し、application flow は use case が所有し、transport は use case を HTTP や将来の entry point に適合させます。

このガイドでは、Kawa application 開発用の公開Skillの使い方と、エージェントが生成した Kawa application 変更を人間が確認するときの観点を説明します。

## Public Skill

このリポジトリには、Kawa application を作るエージェント向けの公開 Codex Skill が含まれます。

- [`skills/kawa-app-developer/`](../skills/kawa-app-developer/)

Codex Skill をサポートする agent environment に、このSkillを install または copy してください。ローカルの Codex で使う場合、通常の配置先は次です。

```text
~/.codex/skills/kawa-app-developer/
```

このSkillは Kawa を利用する application 向けです。Kawa 本体を変更するためのものではありません。Framework maintenance では maintainer-oriented skill を使ってください。

## What Agents Should Read

Kawa application を変更する前に、エージェントは application を確認し、次を読むべきです。

- [仕様書](specification.ja.md)
- [Rails-like Convention Proposal 日本語版](rails-like-conventions.ja.md)
- application 既存の `Program.cs`、endpoint file、use case file、tests
- 関連する `skills/kawa-app-developer/references/` の file

Public API 変更では、application が該当 endpoint を map している場合、OpenAPI または `/kawa/catalog.json` も確認してください。

## How To Ask An Agent

よい依頼は、use case と期待する contract shape を明示します。

```text
Use $kawa-app-developer to add a CreateUser Kawa use case with request fields name and email, a validation failure when email is missing, a POST /users endpoint, and focused tests.
```

既存 application では、期待する area や folder も含めます。

```text
Use $kawa-app-developer to add a Billing/CreateInvoice use case following the existing Contracts, UseCases, and Endpoints/Web layout.
```

Cross-language application では、boundary を明示します。

```text
Use $kawa-app-developer to wire the F# CreateUserUseCase through explicit C# friendly request and response contract types.
```

## Human Review Checklist

エージェントが生成した Kawa application 変更を確認するときは、次を見てください。

- 変更が endpoint logic ではなく、request/response contracts と use cases から始まっている。
- Use case が `KawaResult<TResponse>` を返している。
- 予測可能な failure が `KawaError` と `KawaErrorKind` を使っている。
- Use case が ASP.NET Core、`HttpContext`、HTTP status code、CLI parser type、Worker SDK type、RPC context type に依存していない。
- Endpoint が薄く保たれ、`MapKawaPost` を使っている。
- API documentation が重要な public use case には、有用な `KawaUseCaseAttribute` と `KawaErrorResponseAttribute` metadata がある。
- Tests が use case の success と failure behavior を cover している。
- Contract shape または metadata が変わる場合、OpenAPI または `/kawa/catalog.json` が確認されている。

## Related References

- [仕様書](specification.ja.md)
- [Rails-like Convention Proposal 日本語版](rails-like-conventions.ja.md)
- [C# sample](../samples/Kawa.Sample.CSharp/)
- [F# sample](../samples/Kawa.Sample.FSharp/)
- [VB.NET sample](../samples/Kawa.Sample.VB/)

