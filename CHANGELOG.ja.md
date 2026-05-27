# 変更履歴

Kawa の主な変更点をここに記録します。

## [0.3.0] - 未リリース

### 変更

- map 済み use case contract を transport 非依存の catalog に変換する Kawa API catalog document endpoint
  `/kawa/catalog.json` を追加しました。
- F# sample の名前を `Kawa.Sample.Mixed` から `Kawa.Sample.FSharp` に変更しました。
- F# と VB.NET の sample を `1ユースケース = 1ファイル` の規約に合わせました。
- OpenAPI metadata で、Kawa の unknown failure を HTTP result mapper と一致する
  `ProblemDetails` / `application/problem+json` response として記述するようにしました。
- テストとカバレッジ関連の依存関係を更新しました。
  - `coverlet.collector` を `10.0.1` に更新
  - `Microsoft.NET.Test.Sdk` を `18.5.1` に更新
  - `xunit` を `2.9.3` に更新
  - `xunit.runner.visualstudio` を `3.1.5` に更新
  - `Microsoft.AspNetCore.TestHost` を `10.0.8` に更新
- CI とリリース workflow で使う GitHub Actions を更新しました。

### インフラ

- Dependabot PR のカバレッジアップロードで、Codecov に正しい token が渡るようにしました。

## [0.2.1] - 2026-05-27

### 追加

- Kawa Web アプリケーション向けに OpenAPI JSON endpoint 対応を追加しました。
- 開発時の API ドキュメント用に Swagger UI と ReDoc の mapping helper を追加しました。
- use case attribute と error response metadata から OpenAPI operation metadata を生成するようにしました。

## [0.2.0] - 2026-05-26

### 追加

- use case metadata attribute と catalog support を追加しました。
- transport に依存しない API 記述のため、error response metadata を追加しました。
- transport mapper abstraction と HTTP mapper 実装を追加しました。
- Rails-like convention proposal の英語版と日本語版ドキュメントを追加しました。

### 変更

- request / response contract を use case の中にまとめる形へ sample を更新しました。
- contract、catalog、web integration に関する設計思想ドキュメントを拡充しました。

## [0.1.1] - 2026-05-25

### 追加

- VB.NET boundary sample と language boundary test を追加しました。
- NuGet release workflow と local packaging script を追加しました。

[0.3.0]: https://github.com/huideyeren/Kawa/compare/v0.2.1...HEAD
[0.2.1]: https://github.com/huideyeren/Kawa/compare/v0.2.0...v0.2.1
[0.2.0]: https://github.com/huideyeren/Kawa/compare/v0.1.1...v0.2.0
[0.1.1]: https://github.com/huideyeren/Kawa/releases/tag/v0.1.1
