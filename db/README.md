# Oracleの初回準備

提出用Composeは `library-training` プロジェクト、公開ポート1521を使う。元の開発DBとは別のコンテナ・ボリュームになる。以下は新しい空の専用環境で一度だけ行う。

## 起動

リポジトリのルートで実行する。

```powershell
docker compose up -d oracle
docker compose ps
docker compose logs --tail 40 oracle
```

初回はイメージ取得とDB準備に時間がかかる。ログの準備完了表示・コンテナのhealthyを確認する。Composeだけでは業務用のユーザー・表・データは作られない。

## ユーザーと表の作成

DBeaver等で `localhost:1521/FREEPDB1` へSYSTEMとして接続する。管理パスワードは `compose.yaml` のORACLE_PWD（新規ローカル環境用のサンプル値）。接続形式はSIDではなくサービス名FREEPDB1。

以下のプレースホルダーを、それぞれ自分のローカル用パスワードに置き換えて実行する。

```sql
CREATE USER LIBRARY IDENTIFIED BY "YOUR_LIBRARY_PASSWORD"
    DEFAULT TABLESPACE USERS QUOTA 20M ON USERS;
GRANT CREATE SESSION, CREATE TABLE TO LIBRARY;
CREATE USER LIBRARY_TEST IDENTIFIED BY "YOUR_TEST_PASSWORD"
    DEFAULT TABLESPACE USERS QUOTA 20M ON USERS;
GRANT CREATE SESSION, CREATE TABLE TO LIBRARY_TEST;
```

1. LIBRARYユーザーの接続を作り、`SELECT USER FROM DUAL;` がLIBRARYになることを確認する。
2. `migrations/001_library.sql` を「SQLスクリプトを実行」（DBeaverではAlt+X）で適用する。
3. 同じLIBRARY接続で `seeds/001_catalog.sql` を適用する。
4. LIBRARY_TESTユーザーの接続でもDDLを適用する。**テスト用スキーマにはseedを手動投入しない。**

SQLの区切りは `;`、空行を区切りにしない。seed実行は末尾でコミットし、エラー時に停止してロールバックする設定を使う。DDLは自動コミットされるため、途中失敗時は作成済みの表を確認し、未実行の文から再開する。既存の表・データの全削除は行わない。

作成する5表：BOOK / BOOK_COPY / MEMBER / APP_USER / LOAN。書籍情報と実物の一冊を分け、未返却の貸出から貸出可否を求める。APP_USERの架空データはパスワードハッシュ未設定で、ログイン機能はまだない。

```sql
SELECT COUNT(*) FROM BOOK_COPY;
-- LIBRARYでは5、LIBRARY_TESTでは0
```

SQL*Plus/SQLclを使う場合は、接続後に `WHENEVER SQLERROR EXIT SQL.SQLCODE ROLLBACK` を設定してからDDL/seedを実行する。このクライアント専用命令はDBeaverへ通常のSQLとして送らない。

## APIとテストの接続設定

APIのキーは `ConnectionStrings:LibraryOracle`。Visual Studioの「ユーザーシークレットの管理」か、ルートから次のコマンドで設定する。ポートを変更した場合は接続文字列も合わせる。

```powershell
dotnet user-secrets set "ConnectionStrings:LibraryOracle" "User Id=LIBRARY;Password=YOUR_LIBRARY_PASSWORD;Data Source=localhost:1521/FREEPDB1;Connection Timeout=5;" --project LibraryTraining.Api
```

OracleテストはLIBRARY_TESTへ接続する。テストのプロセスで環境変数を設定する。

```powershell
$env:ConnectionStrings__LibraryOracleTest = 'User Id=LIBRARY_TEST;Password=YOUR_TEST_PASSWORD;Data Source=localhost:1521/FREEPDB1;Connection Timeout=5;'
$env:Logging__EventLog__LogLevel__Default = 'None'
dotnet test LibraryTraining.OracleTests/LibraryTraining.OracleTests.csproj
```

テストはログインユーザーとCURRENT_SCHEMAがLIBRARY_TESTであること、5表が存在して空であることを確認する。準備不足は失敗として報告する。fixtureはトランザクションで投入し、テストが作った固定IDのみを後始末する。同じスキーマで複数のランナーを同時起動しない。

## よくある準備時の問題

- 500：接続設定不足、表の不足、認証失敗等。API側のログと接続ユーザーを確認する。SYSTEMに表を作ってもLIBRARYとは別スキーマになる。
- 503：実装で識別した接続タイムアウト・listener停止・接続切断。コンテナの状態、ポート、サービス名を確認する。
- APIへ接続設定を変更した場合は、停止して起動し直す。

参考：[Oracle公式asyncサンプル](https://github.com/oracle/dotnet-db-samples/blob/master/samples/async/async.cs)、[ODP.NET Coreの動作要件](https://docs.oracle.com/en/database/oracle/oracle-database/26/odpnt/InstallSystemRequirements.html)、[DBeaverのSQL実行](https://dbeaver.com/docs/dbeaver/SQL-Execution/)。
