using LibraryTraining.Application.BookCopies;
using LibraryTraining.Infrastructure.BookCopies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Text.Json;

namespace LibraryTraining.Api.Tests
{
    public class BookCopiesTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public BookCopiesTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact(DisplayName = "蔵書一覧は固定5冊のJSON・一冊ごとのID・貸出可否・棚のnullを返す")]
        public async Task GetBookCopies_ReturnsCatalogContract()
        {
            using var factory = WithQuery(new FakeBookCopyQuery());
            using var client = CreateClient(factory);
            using var response = await client.GetAsync("/book-copies");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var rows = json.RootElement.EnumerateArray().ToArray();
            Assert.Equal(5, rows.Length);
            Assert.Equal(new[] { "C-001", "C-002", "C-003", "C-004", "C-005" },
                rows.Select(row => row.GetProperty("accessionNumber").GetString()));
            Assert.Equal(5, rows.Select(row => row.GetProperty("bookCopyId").GetString()).Distinct().Count());
            Assert.Equal(rows[0].GetProperty("bookId").GetString(), rows[1].GetProperty("bookId").GetString());
            Assert.Equal(new[] { "available", "onLoan", "available", "onLoan", "available" },
                rows.Select(row => row.GetProperty("availability").GetString()));
            Assert.Equal(JsonValueKind.Null, rows[4].GetProperty("shelfLocation").ValueKind);

            Assert.All(rows, row =>
            {
                Assert.Equal(8, row.EnumerateObject().Count());
                foreach (var name in new[] { "bookCopyId", "bookId", "accessionNumber", "title", "authorDisplayName", "category", "availability" })
                {
                    Assert.Equal(JsonValueKind.String, row.GetProperty(name).ValueKind);
                    Assert.False(string.IsNullOrWhiteSpace(row.GetProperty(name).GetString()));
                }
                Assert.Contains(row.GetProperty("shelfLocation").ValueKind,
                    new[] { JsonValueKind.String, JsonValueKind.Null });
            });
        }

        [Fact(DisplayName = "取得結果が0冊なら200と空配列を返す")]
        public async Task GetBookCopies_EmptyQueryReturnsEmptyArray()
        {
            using var factory = WithQuery(new TestBookCopyQuery(Array.Empty<BookCopyDto>()));
            using var client = CreateClient(factory);
            using var response = await client.GetAsync("/book-copies");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(JsonValueKind.Array, json.RootElement.ValueKind);
            Assert.Equal(0, json.RootElement.GetArrayLength());
        }

        [Fact(DisplayName = "未対応クエリは取得処理を呼ばず400を返す")]
        public async Task GetBookCopies_RejectsQueryParameters()
        {
            using var factory = WithQuery(new TestBookCopyQuery(new Exception("取得処理を呼んではいけない")));
            using var client = CreateClient(factory);
            using var response = await client.GetAsync("/book-copies?availability=available");

            await AssertProblemAsync(response, HttpStatusCode.BadRequest, "unsupported_query");
        }

        [Theory(DisplayName = "取得失敗は503または500を返し例外の詳細を漏らさない")]
        [InlineData(true, HttpStatusCode.ServiceUnavailable, "catalog_unavailable")]
        [InlineData(false, HttpStatusCode.InternalServerError, "internal_error")]
        public async Task GetBookCopies_MapsFailures(bool temporary, HttpStatusCode status, string code)
        {
            const string privateDetails = "private-connection-and-sql-details";
            Exception failure = temporary
                ? new CatalogUnavailableException(privateDetails)
                : new InvalidOperationException(privateDetails);
            using var factory = WithQuery(new TestBookCopyQuery(failure));
            using var client = CreateClient(factory);
            using var response = await client.GetAsync("/book-copies");

            await AssertProblemAsync(response, status, code);
            Assert.DoesNotContain(privateDetails, await response.Content.ReadAsStringAsync());
        }

        [Fact(DisplayName = "取得順に依存せず蔵書番号と一冊のIDの昇順で返す")]
        public async Task GetBookCopies_SortsRows()
        {
            var rows = new[]
            {
                new BookCopyDto { BookCopyId = "copy-z", AccessionNumber = "C-002" },
                new BookCopyDto { BookCopyId = "copy-b", AccessionNumber = "C-001" },
                new BookCopyDto { BookCopyId = "copy-a", AccessionNumber = "C-001" }
            };
            using var factory = WithQuery(new TestBookCopyQuery(rows));
            using var client = CreateClient(factory);
            using var response = await client.GetAsync("/book-copies");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal(new[] { "copy-a", "copy-b", "copy-z" },
                json.RootElement.EnumerateArray().Select(row => row.GetProperty("bookCopyId").GetString()));
        }

        [Fact(DisplayName = "OpenAPIは蔵書一覧の経路・応答・DTO・棚のnull・貸出可否を記述する")]
        public async Task OpenApi_DescribesBookCopies()
        {
            using var factory = _factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
            using var client = CreateClient(factory);
            using var response = await client.GetAsync("/openapi/v1.json");
            response.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = json.RootElement;
            var operation = root.GetProperty("paths").GetProperty("/book-copies").GetProperty("get");
            Assert.Equal("GetBookCopies", operation.GetProperty("operationId").GetString());
            var responses = operation.GetProperty("responses");
            foreach (var status in new[] { "200", "400", "503", "500" })
            {
                Assert.True(responses.TryGetProperty(status, out _));
            }
            var schema = ResolveSchema(root, responses.GetProperty("200").GetProperty("content")
                .GetProperty("application/json").GetProperty("schema"));
            Assert.Equal("array", schema.GetProperty("type").GetString());
            var item = ResolveSchema(root, schema.GetProperty("items"));
            var properties = item.GetProperty("properties");
            Assert.Equal(8, properties.EnumerateObject().Count());
            Assert.Equal(8, item.GetProperty("required").GetArrayLength());
            Assert.Contains("null", properties.GetProperty("shelfLocation").GetProperty("type")
                .EnumerateArray().Select(type => type.GetString()));
            var availability = ResolveSchema(root, properties.GetProperty("availability"));
            // 文字列だけのenumはtypeを省略しても同じ制約を表す。
            if (availability.TryGetProperty("type", out var enumType))
            {
                Assert.Equal("string", enumType.GetString());
            }
            Assert.All(availability.GetProperty("enum").EnumerateArray(),
                value => Assert.Equal(JsonValueKind.String, value.ValueKind));
            Assert.Equal(new[] { "available", "onLoan" }, availability.GetProperty("enum")
                .EnumerateArray().Select(value => value.GetString()));
        }

        private WebApplicationFactory<Program> WithQuery(IBookCopyQuery query)
        {
            return _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IBookCopyQuery>();
                services.AddSingleton(query);
            }));
        }

        private static HttpClient CreateClient(WebApplicationFactory<Program> factory)
        {
            return factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("http://localhost"),
                AllowAutoRedirect = false
            });
        }

        private static async Task AssertProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code)
        {
            Assert.Equal(status, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.Equal((int)status, json.RootElement.GetProperty("status").GetInt32());
            Assert.Equal(code, json.RootElement.GetProperty("code").GetString());
        }

        private static JsonElement ResolveSchema(JsonElement root, JsonElement schema)
        {
            if (!schema.TryGetProperty("$ref", out var reference)) return schema;
            var resolved = root;
            foreach (var segment in reference.GetString()!.Substring(2).Split('/'))
            {
                resolved = resolved.GetProperty(segment.Replace("~1", "/").Replace("~0", "~"));
            }
            return resolved;
        }

        private sealed class TestBookCopyQuery : IBookCopyQuery
        {
            private readonly IReadOnlyList<BookCopyDto>? _rows;
            private readonly Exception? _failure;

            public TestBookCopyQuery(IReadOnlyList<BookCopyDto> rows) { _rows = rows; }
            public TestBookCopyQuery(Exception failure) { _failure = failure; }

            public Task<IReadOnlyList<BookCopyDto>> GetAllAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return _failure == null
                    ? Task.FromResult(_rows!)
                    : Task.FromException<IReadOnlyList<BookCopyDto>>(_failure);
            }
        }
    }
}
