# LibraryTraining

図書館の蔵書・貸出管理システムのバックエンドを、C#とOracleで実装する学習用プロジェクト。ASP.NET Core Web APIでリクエストを受け付け、Oracleに保存した書籍情報や貸出状況を返す。

現在は、図書館が所蔵する本を一覧で取得できる。書名・著者・ジャンル・棚の場所に加え、各冊が貸出可能か貸出中かを確認できる。同じ書籍を複数冊所蔵する場合も、蔵書番号で一冊ずつ区別する。認証や貸出・返却の登録は今後追加する。

## 使用技術

| 対象 | 技術 |
|---|---|
| バックエンド | ASP.NET Core Web API、.NET 10、C# 7.3 |
| データベース | Oracle Database Free、Docker Compose |
| DBアクセス | ODP.NET Core（Oracle.ManagedDataAccess.Core 23.26.301）、SQL |
| テスト | xUnit、Microsoft.AspNetCore.Mvc.Testing |

## プロジェクト構成

```text
LibraryTraining.slnx
LibraryTraining.Api/               Controller、起動・DI、接続設定
LibraryTraining.Application/       UseCase、DTO、データ取得のinterface
LibraryTraining.Infrastructure/    Oracle接続・SQL、テスト用のFake
LibraryTraining.Api.Tests/         DB接続なしで実行するAPIテスト
LibraryTraining.OracleTests/       実Oracleへ接続するAPIテスト
db/                               テーブル作成SQL、サンプルデータ、DB準備手順
compose.yaml                      Oracleコンテナの設定
```

HTTPの処理、UseCase、DBアクセスを別のプロジェクトに分けている。ApiはApplicationとInfrastructureを参照し、InfrastructureはApplicationを参照する。Applicationに置いた取得用interfaceの実装を、ApiのDI設定で割り当てる。

## 起動方法

Visual Studio 2026の「ASP.NETとWeb開発」ワークロードと.NET 10 SDK、Dockerを実行できる環境が必要。DBの初回準備にはDBeaver、SQL*PlusまたはSQLclを使用する。

1. このリポジトリをクローンする。
2. [DB準備手順](db/README.md)に従い、Oracleを起動してユーザー・テーブル・サンプルデータを作成する。
3. Apiのユーザーシークレットに `ConnectionStrings:LibraryOracle` を設定する。接続文字列の設定例はDB準備手順を参照する。
4. `LibraryTraining.slnx` をVisual Studioで開き、ソリューションをビルドする。
5. `LibraryTraining.Api` をスタートアッププロジェクトに設定し、`http` プロファイルで実行する。

コマンドラインから起動する場合は、リポジトリのルートで以下を実行する。

```powershell
dotnet build LibraryTraining.slnx
dotnet run --project LibraryTraining.Api --launch-profile http
```

Oracleの接続先は `localhost:1521/FREEPDB1`。Composeでコンテナを起動した後、DB準備手順に従ってテーブルとデータを作成する。

## API

APIの起動後、PostmanなどのAPIクライアントから `http://localhost:5199` へリクエストを送る。Visual Studioでは [LibraryTraining.Api.http](LibraryTraining.Api/LibraryTraining.Api.http) も使用できる。

| メソッド・パス | 内容 |
|---|---|
| `GET /book-copies` | 所蔵する本の情報と、各冊の貸出状況を一覧で返す |
| `GET /WeatherForecast` | ASP.NET Coreの初期テンプレートのサンプル。ランダムな気温の予報を返す |
| `GET /openapi/v1.json` | API仕様をJSONで返す。Development環境で利用できる |

蔵書一覧の `availability` は、貸出可能なら `available`、貸出中なら `onLoan`。返却済みの貸出履歴があっても貸出中にはせず、未返却の貸出があるかどうかで判定する。棚が未設定の場合、`shelfLocation` はnullになる。

初期データは5冊で、貸出可能が3冊、貸出中が2冊。検索・ページングにはまだ対応していない。

## テスト

DB接続なしのAPIテストは、データ取得処理をFakeに差し替え、ControllerからJSONレスポンスまでを確認する。

```powershell
dotnet test LibraryTraining.Api.Tests/LibraryTraining.Api.Tests.csproj
```

WindowsのEventLogへの書き込み権限で失敗する場合は、実行するターミナルで `$env:Logging__EventLog__LogLevel__Default = 'None'` を設定する。

Oracleを使うテストは、[DB準備手順](db/README.md)に従って専用の `LIBRARY_TEST` スキーマと接続設定を用意し、個別に実行する。

```powershell
dotnet test LibraryTraining.OracleTests/LibraryTraining.OracleTests.csproj
```

## 参考資料

- [プロジェクト構成の参考記事](https://medium.com/@orbens/the-ultimate-guide-to-structuring-scalable-net-projects-from-startup-to-enterprise-c72dae562d1b)
- [ASP.NET Coreの依存関係の挿入](https://learn.microsoft.com/ja-jp/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-10.0)
- [ASP.NET Coreの統合テスト](https://learn.microsoft.com/ja-jp/aspnet/core/test/integration-tests?view=aspnetcore-10.0)
- [Oracle公式の非同期処理サンプル](https://github.com/oracle/dotnet-db-samples/blob/master/samples/async/async.cs)
