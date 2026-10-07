using Microsoft.AspNetCore.Mvc.Testing;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace LibraryTraining.Api.Tests
{
    public class WeatherForecastTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public WeatherForecastTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact(DisplayName = "天気予報APIは200 OKを返す")]
        public async Task GetWeatherForecast_ReturnsOk()
        {
            using var client = CreateClient();
            using var response = await client.GetAsync("/WeatherForecast");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact(DisplayName = "天気予報APIはJSON形式の配列を返す")]
        public async Task GetWeatherForecast_ReturnsJsonArray()
        {
            using var client = CreateClient();
            using var response = await client.GetAsync("/WeatherForecast");

            // メディアタイプの確認
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

            var body = await response.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(body);
            Assert.Equal(JsonValueKind.Array, json.RootElement.ValueKind);
        }

        [Fact(DisplayName = "天気予報APIは予報を5件返す")]
        public async Task GetWeatherForecast_ReturnsFiveForecasts()
        {
            using var json = await GetForecastsAsync();
            var forecasts = json.RootElement;

            Assert.Equal(JsonValueKind.Array, forecasts.ValueKind);
            Assert.Equal(5, forecasts.GetArrayLength());
        }

        [Fact(DisplayName = "各予報のプロパティが期待通りの型である")]
        public async Task GetWeatherForecast_ReturnsExpectedPropertyTypes()
        {
            using var json = await GetForecastsAsync();
            var forecasts = json.RootElement;

            Assert.Equal(JsonValueKind.Array, forecasts.ValueKind);
            Assert.NotEmpty(forecasts.EnumerateArray());
            Assert.All(forecasts.EnumerateArray(), forecast =>
            {
                Assert.Equal(JsonValueKind.Object, forecast.ValueKind);
                Assert.Equal(JsonValueKind.String, forecast.GetProperty("date").ValueKind);
                Assert.Equal(JsonValueKind.String, forecast.GetProperty("summary").ValueKind);
                Assert.Equal(JsonValueKind.Number, forecast.GetProperty("temperatureC").ValueKind);
                Assert.Equal(JsonValueKind.Number, forecast.GetProperty("temperatureF").ValueKind);
            });
        }

        [Fact(DisplayName = "各予報の日付が日付型である")]
        public async Task GetWeatherForecast_ReturnsParseableDates()
        {
            using var json = await GetForecastsAsync();
            var forecasts = json.RootElement;

            Assert.Equal(JsonValueKind.Array, forecasts.ValueKind);
            Assert.NotEmpty(forecasts.EnumerateArray());
            Assert.All(forecasts.EnumerateArray(), forecast =>
            {
                var date = forecast.GetProperty("date").GetString();
                Assert.True(
                    DateOnly.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.None, out _),
                    $"日付として読めない値: {date}"
                );
            });
        }

        [Fact(DisplayName = "各予報の華氏と標準換算値の差が1度以下である")]
        public async Task GetWeatherForecast_ReturnsFahrenheitWithinOneDegree()
        {
            using var json = await GetForecastsAsync();
            var forecasts = json.RootElement;

            Assert.Equal(JsonValueKind.Array, forecasts.ValueKind);
            Assert.NotEmpty(forecasts.EnumerateArray());
            Assert.All(forecasts.EnumerateArray(), forecast =>
            {
                var temperatureC = forecast.GetProperty("temperatureC").GetDouble();
                var temperatureF = forecast.GetProperty("temperatureF").GetDouble();
                // 標準の換算式を使い、サンプルAPIの近似と整数化による誤差を許容する
                var expectedF = temperatureC * 9.0 / 5.0 + 32;
                Assert.InRange(Math.Abs(temperatureF - expectedF), 0.0, 1.0);
            });
        }

        [Fact(DisplayName = "存在しないURLへのGETは404 Not Foundを返す")]
        public async Task GetUnknownUrl_ReturnsNotFound()
        {
            using var client = CreateClient();
            using var response = await client.GetAsync("/WeatherForecast-not-found");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        private HttpClient CreateClient()
        {
            return _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("http://localhost"),
                AllowAutoRedirect = false
            });
        }

        private async Task<JsonDocument> GetForecastsAsync()
        {
            using var client = CreateClient();
            using var response = await client.GetAsync("/WeatherForecast");
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(body);
        }
    }
}
