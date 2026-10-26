using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace MiniCambio.Rates.IntegrationTests;

public class QuotesEndpointsTests(RatesApiFactory factory)
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task CreateQuote_ValidRequest_Returns201WithLocationAndQuote()
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/quotes",
            new { currency = "USD", side = "Buy", foreignAmount = 100m },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var quote = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        var id = quote.GetProperty("id").GetGuid();
        Assert.Equal($"/api/v1/quotes/{id}", response.Headers.Location?.OriginalString);
        Assert.Equal("Buy", quote.GetProperty("side").GetString());
        Assert.Equal(5.427m, quote.GetProperty("rate").GetDecimal());
        Assert.Equal(542.70m, quote.GetProperty("brlAmount").GetDecimal());
        Assert.False(quote.GetProperty("isExpired").GetBoolean());

        var expiresAt = quote.GetProperty("expiresAt").GetDateTimeOffset();
        var createdAt = quote.GetProperty("createdAt").GetDateTimeOffset();
        Assert.Equal(TimeSpan.FromSeconds(60), expiresAt - createdAt);
    }

    [Fact]
    public async Task CreateQuote_InvalidRequest_Returns400WithFieldErrors()
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/quotes",
            new { currency = "usd", side = "Hold", foreignAmount = 0m },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal("Dados inválidos", problem.GetProperty("title").GetString());
        var errors = problem.GetProperty("errors");
        Assert.True(errors.TryGetProperty("currency", out _));
        Assert.True(errors.TryGetProperty("side", out _));
        Assert.True(errors.TryGetProperty("foreignAmount", out _));
    }

    [Fact]
    public async Task CreateQuote_MalformedJson_Returns400ProblemDetails()
    {
        using var content = new StringContent("{ \"currency\": ", Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/api/v1/quotes", content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal("Requisição inválida", problem.GetProperty("title").GetString());
    }

    [Fact]
    public async Task CreateQuote_UnsupportedCurrency_Returns422()
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/quotes",
            new { currency = "XYZ", side = "Sell", foreignAmount = 10m },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal("A moeda XYZ não é suportada.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task GetQuote_Existing_Returns200NotExpired()
    {
        var id = await CreateQuoteAsync(client);

        var response = await client.GetAsync($"/api/v1/quotes/{id}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var quote = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal(id, quote.GetProperty("id").GetGuid());
        Assert.False(quote.GetProperty("isExpired").GetBoolean());
        Assert.False(string.IsNullOrWhiteSpace(quote.GetProperty("servedBy").GetString()));
    }

    [Fact]
    public async Task GetQuote_AfterTtl_ReturnsIsExpiredTrue()
    {
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        await using var timedFactory = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(time);
        }));
        var timedClient = timedFactory.CreateClient();
        var id = await CreateQuoteAsync(timedClient);

        time.Advance(TimeSpan.FromSeconds(61));
        var quote = await timedClient.GetFromJsonAsync<JsonElement>($"/api/v1/quotes/{id}", TestContext.Current.CancellationToken);

        Assert.True(quote.GetProperty("isExpired").GetBoolean());
    }

    [Fact]
    public async Task GetQuote_Unknown_Returns404()
    {
        var response = await client.GetAsync($"/api/v1/quotes/{Guid.NewGuid()}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<Guid> CreateQuoteAsync(HttpClient httpClient)
    {
        var response = await httpClient.PostAsJsonAsync(
            "/api/v1/quotes",
            new { currency = "EUR", side = "Sell", foreignAmount = 50m },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var quote = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        return quote.GetProperty("id").GetGuid();
    }
}
