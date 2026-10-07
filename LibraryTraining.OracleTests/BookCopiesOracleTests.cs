using LibraryTraining.Api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;
using System.Net;
using System.Text.Json;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace LibraryTraining.OracleTests
{
    // 空のLIBRARY_TEST専用。DDLは事前適用し、アプリの開発スキーマと分ける。
    public class BookCopiesOracleTests : IAsyncLifetime
    {
        private string _connectionString = "";
        private bool _seedStarted;

        public async Task InitializeAsync()
        {
            _connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__LibraryOracleTest") ?? "";
            if (string.IsNullOrWhiteSpace(_connectionString))
                throw new InvalidOperationException("OracleテストにはConnectionStrings__LibraryOracleTestの設定が必要です。");

            using var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync();
            using var identity = new OracleCommand("SELECT USER, SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA') FROM DUAL", connection);
            using var reader = await identity.ExecuteReaderAsync();
            await reader.ReadAsync();
            if (reader.GetString(0) != "LIBRARY_TEST" || reader.GetString(1) != "LIBRARY_TEST")
                throw new InvalidOperationException("OracleテストはLIBRARY_TESTユーザーとスキーマでのみ実行できます。");
            reader.Close();

            foreach (var table in new[] { "LOAN", "APP_USER", "MEMBER", "BOOK_COPY", "BOOK" })
            {
                // 表名は上記の固定リストのみ。想定外データを削除せず準備失敗にする。
                using var count = new OracleCommand("SELECT COUNT(*) FROM " + table, connection);
                Assert.Equal(0, Convert.ToInt32(await count.ExecuteScalarAsync()));
            }
        }

        [Fact(DisplayName = "実OracleとAPIで五冊・履歴による重複なし・貸出可否・棚のnullを確認")]
        public async Task GetBookCopies_ReturnsOracleCatalog()
        {
            await SeedAsync();
            using var factory = CreateFactory();
            using var client = CreateClient(factory);
            using var response = await client.GetAsync("/book-copies");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var rows = json.RootElement.EnumerateArray().ToArray();
            Assert.Equal(new[] { "copy-001", "copy-002", "copy-003", "copy-004", "copy-005" },
                rows.Select(row => row.GetProperty("bookCopyId").GetString()));
            Assert.Equal(new[] { "C-001", "C-002", "C-003", "C-004", "C-005" },
                rows.Select(row => row.GetProperty("accessionNumber").GetString()));
            Assert.Equal(new[] { "available", "onLoan", "available", "onLoan", "available" },
                rows.Select(row => row.GetProperty("availability").GetString()));
            Assert.Equal(rows[0].GetProperty("bookId").GetString(), rows[1].GetProperty("bookId").GetString());
            Assert.Equal("図書館サンプルA", rows[0].GetProperty("title").GetString());
            Assert.Equal("サンプル著者A", rows[0].GetProperty("authorDisplayName").GetString());
            Assert.Equal("技術", rows[0].GetProperty("category").GetString());
            Assert.Equal("A-01", rows[0].GetProperty("shelfLocation").GetString());
            Assert.Equal(JsonValueKind.Null, rows[4].GetProperty("shelfLocation").ValueKind);
        }

        [Fact(DisplayName = "空の実OracleからAPIを通して200と空配列を返す")]
        public async Task GetBookCopies_EmptyOracleReturnsEmptyArray()
        {
            using var factory = CreateFactory();
            using var client = CreateClient(factory);
            using var response = await client.GetAsync("/book-copies");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(JsonValueKind.Array, json.RootElement.ValueKind);
            Assert.Equal(0, json.RootElement.GetArrayLength());
        }

        private WebApplicationFactory<Program> CreateFactory()
        {
            return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
                builder.ConfigureAppConfiguration((context, configuration) =>
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:LibraryOracle"] = _connectionString
                    })));
        }

        private static HttpClient CreateClient(WebApplicationFactory<Program> factory)
        {
            return factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("http://localhost"), AllowAutoRedirect = false
            });
        }

        private async Task SeedAsync()
        {
            var script = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Fixtures", "001_catalog.sql"));
            var sql = string.Join("\n", script.Split('\n').Where(line =>
                !line.TrimStart().StartsWith("--") && !line.TrimStart().StartsWith("WHENEVER")));
            using var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            _seedStarted = true;
            foreach (var statement in sql.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                if (statement.Trim().Equals("COMMIT", StringComparison.OrdinalIgnoreCase)) continue;
                using var command = new OracleCommand(statement.Trim(), connection) { BindByName = true };
                await command.ExecuteNonQueryAsync();
            }
            transaction.Commit();
        }

        public async Task DisposeAsync()
        {
            if (!_seedStarted) return;
            using var connection = new OracleConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            var fixtures = new (string Table, string Column, string[] Ids)[]
            {
                ("LOAN", "LOAN_ID", new[] { "loan-001", "loan-002", "loan-003", "loan-004", "loan-005" }),
                ("APP_USER", "USER_ID", new[] { "staff-a", "user-a", "user-b" }),
                ("MEMBER", "MEMBER_ID", new[] { "member-a", "member-b" }),
                ("BOOK_COPY", "BOOK_COPY_ID", new[] { "copy-001", "copy-002", "copy-003", "copy-004", "copy-005" }),
                ("BOOK", "BOOK_ID", new[] { "book-a", "book-b", "book-c", "book-d" })
            };
            foreach (var fixture in fixtures)
            {
                foreach (var id in fixture.Ids)
                {
                    using var command = new OracleCommand(
                        "DELETE FROM " + fixture.Table + " WHERE " + fixture.Column + " = :id", connection) { BindByName = true };
                    command.Parameters.Add("id", OracleDbType.Varchar2).Value = id;
                    await command.ExecuteNonQueryAsync();
                }
            }
            transaction.Commit();
        }
    }
}
