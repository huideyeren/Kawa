# 0.5.0 公開前確認

最終レビューで挙がった実装上の必須修正は対応済み。2026-11-25 に、監査を有効にした restore・全体テスト・Release パッケージ作成がすべて成功したとのユーザー報告を受領。CI と Aspire 実起動の確認が残る。タグ作成・push・NuGet 公開は実施していない。

## 修正と回帰検証

| 課題 | 修正 | 検証 |
| --- | --- | --- |
| 同じ DTO を共有する RPC が別の処理を呼ぶ | 実装型を識別して現在のスコープから解決。具体的な型を優先し、従来のインターフェース登録と最後の登録による上書きも維持 | MagicOnion / protobuf の実通信で異なる処理を呼び分けるテスト |
| protobuf のフィールド欠落・複合型・名前空間 | 正規表現を Roslyn の構文・型解析へ置換。到達可能な DTO グラフを解析し bytes・enum・入れ子・反復要素を明示変換 | 別名・異なる名前空間の DTO、複数プロパティ、bytes・enum・入れ子・List・配列の往復通信。後続プロパティの番号未指定・重複・予約番号・未対応型の診断 |
| MagicOnion の不正宣言・生成名衝突 | KAWA5001〜5004 の診断、名前空間を含む生成ファイル名、型付きクライアント登録 factory | 曖昧な契約・不正名・未対応ペイロード・不完全な Hub・操作重複・別名前空間の同名サービスを検証 |
| Hub のキャンセル | 操作開始前・完了後にキャンセルを確認 | キャンセル済みの送信・配信・参加・離脱が実行されないことを検証 |
| MapGroup の Catalog 経路 | 最終 RouteEndpoint の経路を利用 | `/api/users` の接頭辞を含む JSON を検証 |

Core の失敗後のストリーム終了、既存エラー列挙値（特に Unknown=5）、従来の Catalog JSON・コンストラクター、HTTP/OpenAPI の整合、公開前の版・変更履歴検査も維持する。

## 実装上の条件

- RPC の具体的な型の登録を推奨。インターフェース登録へのフォールバックは同じ契約の登録を列挙して対象実装を選ぶ。
- メッセージは public な引数なしコンストラクターと、public の書き込み可能フィールドまたは get/set/init プロパティを持ち、各メンバーの番号を明示する。未対応型は診断して停止する。詳細は日英仕様を参照。
- Hub の基盤グループ API 自体はキャンセルを受け付けない。開始済みの操作は完了を待ち、キャンセルを通知する。送信済みイベントを取り消すものではない。
- 明示的な MSBuild サービスモデルは ImplementationType を指定すれば実装型解決を利用できる。省略した従来モデルは契約ベースの解決を維持する。属性付きソースの検出では実装型を必ず付与する。
- Roslyn とその notices はビルドタスクの tools/net10.0 に同梱し、アプリの実行時依存へ公開しない。

## 検証状況

- Debug 全ソリューションビルド成功。エラー 0。Aspire サンプルの CLI bundle 未使用による ASPIRE010 が 1 件。
- 実 xUnit エンジンを各アセンブリの deps/runtimeconfig で実行し、151 件成功、失敗・スキップ 0。通常の vstest 実行と同一とは扱わない。
- 0.5.0 実パッケージから 4 テンプレートを新規生成・復元・ビルドし、CLI とエクスポートツールを検証。ビルドタスクの DLL 読み込みとターゲットの実行順序も新規プロジェクトで確認。
- リリースタグ入力 6 件、公開前メタデータ 3 件、テンプレート同期、差分の空白検査が成功。
- ローカル restore はキャッシュを用い監査を無効にした機能検証。オンライン脆弱性監査の成功とは扱わない。

## 通常環境での確認（ユーザー報告により成功）

2026-11-25 に、次の restore・test・pack がすべて通ったとの報告を受領。実行ログと警告内訳そのものは未確認。現在の依存関係を全件監査して警告をエラーにする条件は、CI でも確認する。

再実行が必要になった場合のコマンド：

```bash
cd /Users/iosif/kawa/.worktrees/kawa-050-implementation
dotnet restore Kawa.sln --disable-build-servers -p:NuGetAudit=true -p:NuGetAuditMode=all -p:NuGetAuditLevel=low "-warnaserror:NU1901;NU1902;NU1903;NU1904"
dotnet test Kawa.sln --no-restore --disable-build-servers
bash eng/pack.sh
```

- .NET 10 を主開発に維持。.NET 11 互換性は専用 CI の実行結果で判断する。
- Aspire の実 AppHost 起動は未確認。
- 日英変更履歴は `0.5.0 - 2026-11-25` として公開準備済み。公開日が変わる場合はタグ作成前に日付を揃える。

## 生成パッケージ確認（2026-11-25）

`eng/pack.sh` の対象 14 件と生成済み nupkg の ID が完全一致し、すべて version=0.5.0、Kawa 内部の依存も 0.5.0。シンボルパッケージは 11 件（Tool・Templates・MagicOnion analyzer は対象外）。

| パッケージ | バージョン |
| --- | --- |
| Kawa.Abstractions | 0.5.0 |
| Kawa.Core | 0.5.0 |
| Kawa.Web | 0.5.0 |
| Kawa.FSharp | 0.5.0 |
| Kawa.MagicOnion | 0.5.0 |
| Kawa.MagicOnion.Generators | 0.5.0 |
| Kawa.Grpc | 0.5.0 |
| Kawa.Grpc.Generators | 0.5.0 |
| Kawa.Grpc.ProtoAdapter | 0.5.0 |
| Kawa.Cli | 0.5.0 |
| Kawa.Aspire | 0.5.0 |
| Kawa.Aspire.Hosting | 0.5.0 |
| Kawa.Tool | 0.5.0 |
| Kawa.Templates | 0.5.0 |

ビルドタスク DLL・Roslyn DLL・notices の同梱も確認。各パッケージの SHA-256 と依存情報は `artifacts/packages/kawa-0.5.0-manifest.json` に保存した。この manifest は今回確認した生成物の記録で、公開時に再生成したパッケージのハッシュを保証するものではない。

## Aspire 実起動の確認手順

2026-11-25 時点でこの作業環境から見える SDK・ランタイムは .NET 11.0.100 / 11.0.0 のみ。`global.json` は主開発用の .NET 10 指定を維持している。通常の `dotnet run` は SDK 不在で停止した。ビルド済み DLL を明示的に .NET 11 へ roll forward すると起動処理には入るが、作業環境の設定ディレクトリー書き込み・ソケット作成制限で完了できないため、実起動成功とは扱わない。試行したプロセスは停止済み。

通常ターミナルで .NET 10 SDK が見えることを確認して起動する。

```bash
dotnet --list-sdks
dotnet run --project samples/Kawa.Sample.Aspire.AppHost --no-restore --launch-profile http
```

Dashboard で `chat`・`proto` が Running となり、`magic-cli`・`grpc-cli` が JSON `{"message":"hello"}` を出力して終了コード 0 になることを確認する。Dashboard の認証付き URL は共有不要。確認後はターミナルで Ctrl+C を押して AppHost を終了する。

## コミット・CI への引き継ぎ

2026-11-25 にステージングを試みたが、`.git/worktrees/kawa-050-implementation/index.lock` を作成する権限がなく停止した。ファイル更新は保存済み。コミット・push・タグ作成・公開は未実行。

Aspire 起動確認後、通常のターミナルで差分を確認してコミットする。

```bash
cd /Users/iosif/kawa/.worktrees/kawa-050-implementation
git diff --check
git add -- .github CHANGELOG.md CHANGELOG.ja.md Directory.Build.props Kawa.sln README.md docs eng samples src tests templates
git diff --cached --stat
git commit -m "feat: finalize Kawa 0.5.0 transport integrations"
```

次の PR では .NET 10 の主 CI と .NET 11 の互換性 CI を確認する。タグ作成・NuGet 公開は CI 確認後の段階。
