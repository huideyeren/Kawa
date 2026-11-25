# 変更履歴

Kawa の主な変更点をここに記録します。

## [Unreleased]

## [0.5.0] - 2026-11-25

### 追加

- 通信非依存の server/client/duplex streaming、型付き Hub の契約・Executor と、ペイロードを含まない Activity・Meter 計測。
- MagicOnion のサービス・Hub 生成、4 種の raw RPC、型付き receiver・グループ、生成診断、型付きクライアント factory 登録。
- `obj/` への protobuf スキーマ生成、サーバーアダプター、型解析による DTO グラフ検出、bytes・enum・入れ子・反復型の変換と、既存 proto の明示的アダプター。
- 呼び出しごとのスコープ、JSON 標準出力、診断の標準エラー出力、キャンセル、終了コード・テキスト表示の拡張に対応した `Kawa.Cli`。
- Aspire の ServiceDefaults、ヘルスエンドポイント、サービス検出、AppHost のプロジェクト・参照ヘルパー。
- NuGet パッケージを参照する 4 種の `dotnet new` テンプレート、通信・Aspire サンプル、`kawa grpc export` ツール、隔離されたパッケージ・テンプレート検証。
- 独立した .NET 11 SDK・ランタイム互換性 CI。主開発と主 CI は .NET 10 を維持。

### 変更

- Catalog に任意の通信情報を追加し、従来の位置引数コンストラクターと JSON フィールドを維持。明示した非 HTTP 通信と実際の HTTP マッピングを統合。
- HTTP の追加エラーを OpenAPI と整合し、`Unknown = 5` を含む既存の列挙値を維持。

### 修正

- DTO を共有する操作でも生成 RPC・Hub が正しい実装を解決し、DI の通常の上書き登録を維持。
- Core のストリームを最初の失敗で終了し、列挙子を破棄。
- protobuf DTO の全体と型を解析し、未番号・重複・予約番号・未対応メンバーをデータ欠落させず診断。
- 別の名前空間での生成名衝突を防ぎ、Hub の操作前・非同期完了後にキャンセルを確認。
- Catalog の経路に MapGroup の接頭辞を含め、Aspire CLI サンプルの Host を開始・停止。
- 公開前にリポジトリーとパッケージの版、および日付付き日英変更履歴の一致を要求。

### セキュリティ

- MagicOnion に MessagePack 3.1.9 を要求し、Aspire.Hosting を修正版 13.6.5 へ更新（推移依存の下限に MessagePack 2.5.307 を含む）。
- リリースタグを環境変数経由で検証し、シェルへの入力埋め込みを防止。
- Unknown・未定義エラーの内部詳細を HTTP、RPC status・trailer、CLI 出力で秘匿。

## [0.4.0] - 2026-07-31

### 追加

- Kawa の公開仕様書を英語と日本語で追加しました。
- メンテナと adapter author 向けの内部設計ドキュメントを英語と日本語で追加しました。
- coding agent で Kawa アプリケーションを開発するための公開 agent-development guide を追加しました。
- Kawa メンテナ向けと Kawa アプリケーション開発者向けの配布可能な Codex skill を追加しました。

### 変更

- README から新しい仕様書と agent-development document へリンクしました。
- GitHub Actions の `actions/setup-dotnet` を v6 に更新しました。
- Kawa Web、Swagger UI、ReDoc、ASP.NET Core test host、test SDK の依存関係を更新しました。

### セキュリティ

- NuGet audit の `GHSA-v5pm-xwqc-g5wc` 検出に対応するため、`Microsoft.OpenApi` 2.7.5 を
  明示的な依存関係として追加しました。

## [0.3.1] - 2026-06-23

### 修正

- ネストした `Request` / `Response` contract type が同じ OpenAPI schema reference を共有しないようにし、
  生成クライアントが各 use case に対応する schema を受け取れるようにしました。
- contract assembly が XML documentation file を生成している場合に、contract type と property の
  XML documentation summary を OpenAPI schema description に反映するようにしました。

## [0.3.0] - 2026-05-28

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
- 0.3.0 の API catalog 変換に対する直接テストを追加し、coverage check を安定させました。

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

[0.4.0]: https://github.com/huideyeren/Kawa/compare/v0.3.1...v0.4.0
[0.3.1]: https://github.com/huideyeren/Kawa/compare/v0.3.0...v0.3.1
[0.3.0]: https://github.com/huideyeren/Kawa/compare/v0.2.1...v0.3.0
[0.2.1]: https://github.com/huideyeren/Kawa/compare/v0.2.0...v0.2.1
[0.2.0]: https://github.com/huideyeren/Kawa/compare/v0.1.1...v0.2.0
[0.1.1]: https://github.com/huideyeren/Kawa/releases/tag/v0.1.1
