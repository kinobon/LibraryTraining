using LibraryTraining.Application.BookCopies;
using LibraryTraining.Infrastructure.BookCopies;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace LibraryTraining.Api.Tests
{
    public class OracleBookCopyQueryTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public OracleBookCopyQueryTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact(DisplayName = "本体はOracle実装を登録し、接続設定不足を空配列にせず500にする")]
        public async Task GetBookCopies_MissingConnectionStringReturnsProblem()
        {
            using var factory = _factory.WithWebHostBuilder(builder =>
                builder.ConfigureAppConfiguration((context, configuration) =>
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:LibraryOracle"] = ""
                    })));
            using var scope = factory.Services.CreateScope();
            Assert.IsType<OracleBookCopyQuery>(scope.ServiceProvider.GetRequiredService<IBookCopyQuery>());
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("http://localhost"),
                AllowAutoRedirect = false
            });
            using var response = await client.GetAsync("/book-copies");
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            var body = await response.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(body);
            Assert.Equal("internal_error", json.RootElement.GetProperty("code").GetString());
            Assert.DoesNotContain("LibraryOracle", body);
        }

        [Fact(DisplayName = "Oracle取得の開始前にキャンセルされたら接続を試みない")]
        public async Task GetAllAsync_CanceledTokenDoesNotConnect()
        {
            var query = new OracleBookCopyQuery("");
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => query.GetAllAsync(cancellation.Token));
        }
    }
}
