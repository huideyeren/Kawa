# Kawa 0.5.0 トランスポートアダプター設計

## 状態

承認済みの設計です。本書は実装開始前に、0.5.0の対象範囲を定めます。

## 目的

Kawa 0.5.0では、MagicOnion gRPC、コマンドラインアプリケーション、.NET Aspireのための、単独で導入可能な一級アダプターを追加します。各アダプターは、アプリケーション層へトランスポート固有の型を持ち込まずに、Kawaのユースケースを再利用できなければなりません。

本リリースにはNuGetパッケージ、`dotnet new`テンプレート、実行可能なサンプル、英日両方の公開・保守者向けドキュメント、自動検証を含めます。

## 非対象

- `Kawa.Web`の既存HTTP動作、OpenAPI出力、API Catalog契約を非互換に変更すること。
- ASP.NET Core、MagicOnion、`System.CommandLine`、Aspireへの依存を`Kawa.Abstractions`または`Kawa.Core`へ追加すること。
- MagicOnionのすべての機能をKawaの抽象化にすること。Interceptor、シリアライザー、認証ミドルウェア、デプロイはアプリケーション／MagicOnionの設定として扱います。
- Worker、Controller、無関係な永続化統合を追加すること。

## パッケージ構成

| パッケージ | 責務 | 依存方向 |
| --- | --- | --- |
| `Kawa.MagicOnion` | 実行時のサーバー／クライアント統合、DI登録、生成アダプターの支援、KawaエラーのgRPC変換。 | Core、Abstractions、MagicOnionに依存。 |
| `Kawa.MagicOnion.Generators` | サービス契約、アダプター、クライアント登録コードのIncremental Source Generatorと診断。 | Analyzer専用の依存。生成コードは`Kawa.MagicOnion`に依存。 |
| `Kawa.Cli` | `System.CommandLine`のコマンド結線、標準入出力、JSON出力、診断、終了コード変換。 | CoreとAbstractionsに依存。 |
| `Kawa.Aspire` | サービス側のKawa観測性、ヘルスチェック、サービスディスカバリー統合。 | 必要なAspireクライアント／テレメトリーパッケージに依存。 |
| `Kawa.Aspire.Hosting` | Kawa MagicOnion／CLIプロジェクト用のAppHostプロジェクトリソース拡張。 | Aspire Hostingのみに依存。 |
| `Kawa.Templates` | CLI、MagicOnion、Aspire分散アプリケーション向けの`dotnet new`テンプレート。 | テンプレートパッケージ。実行時依存なし。 |

`Kawa.Abstractions`はトランスポート非依存のまま保ちます。`Kawa.Core`もトランスポート非依存のままとし、アダプターパッケージを参照しません。`Kawa.Web`はHTTPアダプターのままであり、新しいアダプターの依存先にはしません。

## トランスポート非依存契約

既存の`IUseCase<TRequest, TResponse>`はUnary操作の契約として維持します。

`Kawa.Abstractions`には、次の公開ストリーム契約を追加します。

- `IServerStreamingUseCase<TRequest, TResponse>`: 1件のリクエストを受け、`IAsyncEnumerable<KawaResult<TResponse>>`を生成する。
- `IClientStreamingUseCase<TRequest, TResponse>`: `IAsyncEnumerable<TRequest>`を受け、単一の`Task<KawaResult<TResponse>>`を生成する。
- `IDuplexStreamingUseCase<TRequest, TResponse>`: ストリーム入力を受け、`IAsyncEnumerable<KawaResult<TResponse>>`を生成する。

`Kawa.Core`には、各契約に対応する意図的に薄いExecutorを追加します。Executorが担うのはキャンセルの伝播とnull検証だけであり、トランスポートの関心事は取得しません。

サーバーストリームまたは双方向ストリームの出力中に失敗した`KawaResult<TResponse>`が生じた場合、アダプター固有のエラー形式でトランスポートを終了します。成功メッセージとしてストリームへシリアライズしません。これにより、クライアントがエラーをドメインデータとして扱うことを防ぎます。

## StreamingHub契約

StreamingHubは通常の双方向ストリーミングとして扱わず、型付きのイベント・セッション抽象化としてサポートします。

`Kawa.Abstractions`には次を追加します。

- 接続ライフサイクルとクライアントイベント処理のための`IStreamingHubUseCase<TClientEvent, TServerEvent>`。
- 型付きサーバーイベントの送信、ブロードキャスト、名前付きグループへの参加／離脱、接続IDの読み取りのための`IKawaHubSession<TServerEvent>`。
- トランスポートが予期せず閉じた場合にもアプリケーションが状態を解放できるようにする、接続・切断ライフサイクル契約。

これらの契約はMagicOnionのService Context型やgRPCのStream Reader/Writerを公開しません。`Kawa.MagicOnion`が、MagicOnionの型付きReceiverとGroup APIを用いて`IKawaHubSession<TServerEvent>`を実装します。

## MagicOnion統合と生成

アプリケーションは、Unaryまたはraw streamのユースケースクラスに`[KawaMagicOnionService("service-name")]`を、`IStreamingHubUseCase<TClientEvent, TServerEvent>`の実装に`[KawaMagicOnionHub("hub-name")]`を付けて利用を開始します。`[KawaMagicOnionOperation("operation-name")]`は生成する操作名を任意に上書きします。指定しない場合は、`UseCase`接尾辞を除いたユースケースクラス名を使用します。生成する公開型は、注釈を付けた型の名前空間に`.Generated`を追加した名前空間へ出力します。ジェネレーターは次を提供しなければなりません。

1. Unary、Server Streaming、Client Streaming、Duplex Streamingのための共有MagicOnionサービスインターフェース。
2. 宣言した各Kawa Hubの型付き`IStreamingHub`とReceiverインターフェース。
3. DIから対応するKawaユースケースとExecutorを解決するサーバーアダプタークラス。
4. リフレクションや実行時コード生成ではなく、生成された共有契約を使うクライアント登録ヘルパー。
5. 未対応のシリアライズ可能な型、サービス／操作／イベント名の重複、不完全なHub宣言、曖昧なユースケース契約に対する診断。

ジェネレーターはIncrementalで決定的であり、クライアントとサーバーの両プロジェクトで利用できなければなりません。契約アセンブリは、ASP.NET Coreホストを参照せずに共有できます。

実行時APIは意図的に小さく保ちます。アプリケーションは`AddKawaMagicOnion()`でKawaとMagicOnionサービスを登録し、`MapKawaMagicOnionServices()`で生成済みサービスをマップします。MagicOnion固有のミドルウェア、シリアライザー設定、認証、Interceptorは、アプリケーションが直接設定できます。

raw gRPCは4種類すべての通信形態をサポートします。StreamingHubは、型付きクライアントReceiver、接続ライフサイクル、サーバーからの直接イベント、グループブロードキャスト、名前付きグループへの参加をサポートします。ベンダー固有のMagicOnion拡張をすべて抽象化することは約束しません。

## エラーモデル

既存の`KawaErrorKind`値は意味を変えずに維持します。次のトランスポート非依存の種類を追加します。

| 新しい種類 | 意味 | gRPCステータス | HTTPマッピング |
| --- | --- | --- | --- |
| `Cancelled` | 呼び出し元が操作を取り消した。 | `Cancelled` | 499 Client Closed Request。HTTP接続がすでに閉じている場合はマッピングを送信しない。 |
| `DeadlineExceeded` | 操作が期限までに完了しなかった。 | `DeadlineExceeded` | 504 Gateway Timeout |
| `AlreadyExists` | 作成しようとした対象がすでに存在する。 | `AlreadyExists` | 409 Conflict |
| `PreconditionFailed` | 現在の状態が、操作に明記された前提条件を満たさない。 | `FailedPrecondition` | 412 Precondition Failed |
| `ResourceExhausted` | クォータ、容量、またはレート制限を使い切った。 | `ResourceExhausted` | 429 Too Many Requests |
| `Unavailable` | 一時的な依存先またはサービス障害であり、再試行に意味がある。 | `Unavailable` | 503 Service Unavailable |

`Kawa.MagicOnion`は既存の種類を次のように変換します。`Validation`は`InvalidArgument`、`Unauthorized`は`Unauthenticated`、`Forbidden`は`PermissionDenied`、`NotFound`は`NotFound`、`Conflict`は`Aborted`、`Unknown`は`Internal`です。

MagicOnionトランスポートは、予測可能なエラーに対してUTF-8 JSONでシリアライズした`KawaError`を、バイナリgRPCトレーラー`kawa-error-bin`に保存します。ストリーム中の失敗は、そのステータスとトレーラーでRPCを終了します。Hub操作の失敗は対応するRPC失敗として表し、接続レベルの失敗は同じ変換済みステータスで接続を閉じます。

## CLI統合

`Kawa.Cli`は`System.CommandLine`を中心に実行を標準化します。引数パーサーを置き換えるのではなく、コマンド結線と呼び出しのヘルパーを提供します。

- 成功した構造化結果は、既定でJSONとして標準出力だけに書き込みます。
- 予測可能なエラーと予期しない例外は、標準エラーだけに書き込みます。
- 既定の終了コードは成功が`0`、すべての失敗が`1`です。
- アプリケーション固有の終了コードポリシーには、`IKawaCliExitCodeMapper`を登録できます。
- 人間向け出力にはテキストフォーマッターを登録できますが、既定のJSON契約は変わりません。
- キャンセルトークンはコマンド呼び出しとコンソールのキャンセルから発生し、Kawa Executorへ伝播します。

## Aspire統合

`Kawa.Aspire`はサービス側の統合です。`AddKawaAspireDefaults()`拡張は、Kawa／MagicOnionのActivityとMeterのSourceをOpenTelemetryへ追加し、ASP.NET Coreホストには`/health`と`/alive`のヘルスチェックエンドポイントを登録し、生成したMagicOnionクライアントに必要なサービスディスカバリーを有効にします。

`Kawa.Aspire.Hosting`はAppHost専用です。次のプロジェクトリソース拡張を提供します。

- gRPCエンドポイントとサービスディスカバリー情報を持つMagicOnionサービスの追加。
- 引数、環境変数、明示的な依存関係を持つ、実行可能リソースとしてのCLIプロジェクトの追加。
- コンシューマープロジェクトからKawa MagicOnionサービスを参照する機能。

Hostingパッケージはサービスを自ら実行せず、Kawa.Web、MagicOnion、またはCLIだけを利用するアプリケーションへAspireを強制しません。

## Catalogとドキュメント

現在のHTTP Catalogは互換性を保ちます。各エントリーには、任意の追加フィールドとして`transports`配列を持たせます。配列の値は`http`、`grpc`、`cli`、`streamingHub`の公開形態を識別し、それぞれルート、サービス／操作、コマンドパス、Hub名を含みます。`transports`を読まない既存コンシューマーの動作は変わりません。

実装では、次の英日両方を更新します。

- 公開パッケージ、契約、トランスポートマッピング、生成契約、テンプレート、互換性のための`docs/specification.*`。
- 依存方向、ジェネレーター設計、Catalog拡張、テレメトリー、テスト戦略のための`docs/internal-design.*`。
- 0.5.0リリースノートのための`CHANGELOG.*`。

## テンプレートとサンプル

`Kawa.Templates`には3つのテンプレートを含めます。

1. `System.CommandLine`、JSON出力、整形出力の拡張点、エラー／終了コード動作を備えたCLIアプリケーション。
2. Unary、3種類すべてのraw stream、型付きStreamingHubを示すMagicOnionサーバー／クライアントの組。
3. AppHost、ServiceDefaults、MagicOnionサービス、CLIクライアント／ジョブリソースを含むAspire分散アプリケーション。

実行可能なサンプルはテンプレートと対応させ、小さなチャットドメインを共有します。サンプルは、同じアプリケーション契約をHTTP、MagicOnionの4種類のraw gRPC、StreamingHubのイベント／グループ、CLIコマンドで示します。Aspireサンプルはトポロジーを起動し、ダッシュボードにテレメトリーを出します。

## 検証戦略

- Coreテストは、新しいすべてのStream Executor、キャンセル、終端結果の動作を対象にします。
- MagicOnion統合テストは、Unary、Server／Client／Duplex Streaming、生成契約の利用性、ステータス／トレーラー変換、Hubの接続／切断、イベント配信、グループブロードキャストを対象にします。
- Generatorテストは、生成ソースとすべての診断を検証します。
- CLIテストは、パース統合、標準出力／標準エラーの分離、JSONとカスタム整形、キャンセル、終了コードの差し替えを対象にします。
- Aspireテストは、AppHostリソース参照、サービスディスカバリー設定、ヘルスチェック、出力されるテレメトリーを対象にします。
- テンプレートテストは、各テンプレートを生成し、復元／ビルドし、適切なスモークテストを実行します。
- 既存のCore・Webテストは、0.4互換性の証明として変更せずに実行し続けます。

## 互換性とリリース

0.5.0は後方互換なminorリリースです。既存の公開動作を削除・変更せず、パッケージ、契約、Catalogフィールド、エラー種類を追加します。実装では、公開する全パッケージを`0.5.0`へ一貫して更新し、既存のリリースフローでパッケージ化します。

## 実装順序

1. トランスポート非依存のストリーム・Hub・エラー契約と、Core Executor・テストを追加する。
2. MagicOnion Runtimeパッケージ、Source Generator、生成コードテスト、gRPC／Hub統合テストを追加する。
3. CLI統合、テンプレート、サンプル、テストを追加する。
4. AspireのService／Hostingパッケージ、分散テンプレート／サンプル、AppHostテストを追加する。
5. Catalogを拡張し、英日ドキュメントと変更履歴を更新し、全テンプレートを検証し、ソリューション全体をパッケージ化する。
