using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace MiniCambio.Rates.IntegrationTests;

public class RatesEndpointsTests(RatesApiFactory factory)
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task GetRates_SeededDatabase_ReturnsFourCurrenciesWithSpread()
    {
        var response = await client.GetAsync("/api/v1/rates", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rates = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(["CHF", "EUR", "GBP", "USD"], rates.EnumerateArray().Select(x => x.GetProperty("currency").GetString()));

        var usd = rates.EnumerateArray().Single(x => x.GetProperty("currency").GetString() == "USD");
        Assert.Equal(5.40m, usd.GetProperty("midRate").GetDecimal());
        Assert.Equal(5.427m, usd.GetProperty("buyRate").GetDecimal());
        Assert.Equal(5.373m, usd.GetProperty("sellRate").GetDecimal());
        Assert.False(string.IsNullOrWhiteSpace(usd.GetProperty("servedBy").GetString()));
        Assert.Equal(JsonValueKind.String, usd.GetProperty("quotedAt").ValueKind);
    }

    [Fact]
    public async Task GetRate_KnownCurrencyInLowercase_ReturnsRate()
    {
        var response = await client.GetAsync("/api/v1/rates/gbp", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rate = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal("GBP", rate.GetProperty("currency").GetString());
        Assert.Equal(6.90m, rate.GetProperty("midRate").GetDecimal());
    }

    [Fact]
    public async Task GetRate_UnknownCurrency_Returns404ProblemDetails()
    {
        var response = await client.GetAsync("/api/v1/rates/XYZ", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal("Recurso não encontrado", problem.GetProperty("title").GetString());
        Assert.Contains("XYZ", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Request_WithCorrelationId_EchoesHeader()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/rates");
        request.Headers.Add("X-Correlation-Id", "teste-123");

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal("teste-123", Assert.Single(response.Headers.GetValues("X-Correlation-Id")));
    }

    [Fact]
    public async Task Request_WithoutCorrelationId_GeneratesOne()
    {
        var response = await client.GetAsync("/api/v1/rates", TestContext.Current.CancellationToken);

        Assert.True(Guid.TryParse(Assert.Single(response.Headers.GetValues("X-Correlation-Id")), out _));
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task HealthChecks_DatabaseUp_ReturnHealthy(string path)
    {
        var response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }
}
