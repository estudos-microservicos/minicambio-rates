using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using MiniCambio.Rates.Api.Application;
using MiniCambio.Rates.Api.Application.Quotes;
using MiniCambio.Rates.Api.Application.Rates;
using MiniCambio.Rates.Api.Domain;

namespace MiniCambio.Rates.UnitTests.Application;

public class QuoteHandlersTests
{
    private readonly FakeTimeProvider time = new(new DateTimeOffset(2026, 5, 10, 12, 0, 0, TimeSpan.Zero));
    private readonly InMemoryBaseRateRepository baseRates = new();
    private readonly InMemoryQuoteRepository quotes = new();
    private readonly IOptions<RatesOptions> options =
        Options.Create(new RatesOptions { SpreadPercent = 0.5m, QuoteTtlSeconds = 60 });

    [Fact]
    public async Task CreateQuote_BuySupportedCurrency_PersistsQuoteWithSpreadAndTtl()
    {
        var handler = CreateHandler(variation: 0m);

        var response = await handler.HandleAsync(new CreateQuoteRequest("USD", "Buy", 100m), CancellationToken.None);

        var stored = Assert.Single(quotes.Quotes);
        Assert.Equal(stored.Id, response.Id);
        Assert.Equal(QuoteSide.Buy, response.Side);
        Assert.Equal(5.427m, response.Rate);
        Assert.Equal(542.70m, response.BrlAmount);
        Assert.Equal(time.GetUtcNow().AddSeconds(60), response.ExpiresAt);
        Assert.False(response.IsExpired);
    }

    [Fact]
    public async Task CreateQuote_SellWithVariation_AppliesVariationBeforeSpread()
    {
        var handler = CreateHandler(variation: 0.01m);

        var response = await handler.HandleAsync(new CreateQuoteRequest("USD", "sell", 10m), CancellationToken.None);

        // mid ao vivo = 5,40 * 1,01 = 5,454; venda = 5,454 * 0,995 = 5,42673
        Assert.Equal(QuoteSide.Sell, response.Side);
        Assert.Equal(5.42673m, response.Rate);
        Assert.Equal(54.27m, response.BrlAmount);
    }

    [Fact]
    public async Task CreateQuote_UnsupportedCurrency_ThrowsUnsupportedCurrencyException()
    {
        var handler = CreateHandler();

        var exception = await Assert.ThrowsAsync<UnsupportedCurrencyException>(
            () => handler.HandleAsync(new CreateQuoteRequest("XYZ", "Buy", 100m), CancellationToken.None));

        Assert.Equal("XYZ", exception.Currency);
        Assert.Empty(quotes.Quotes);
    }

    [Fact]
    public async Task GetQuote_AfterTtl_ReturnsExpired()
    {
        var created = await CreateHandler().HandleAsync(new CreateQuoteRequest("EUR", "Buy", 1m), CancellationToken.None);
        var handler = new GetQuoteHandler(quotes, time);

        time.Advance(TimeSpan.FromSeconds(59));
        Assert.False((await handler.HandleAsync(created.Id, CancellationToken.None)).IsExpired);

        time.Advance(TimeSpan.FromSeconds(1));
        Assert.True((await handler.HandleAsync(created.Id, CancellationToken.None)).IsExpired);
    }

    [Fact]
    public async Task GetQuote_UnknownId_ThrowsResourceNotFound()
    {
        var handler = new GetQuoteHandler(quotes, time);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => handler.HandleAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task GetRates_ReturnsBuyAndSellAroundLiveMidRate()
    {
        var handler = new GetRatesHandler(baseRates, Calculator(0m), time);

        var rates = await handler.HandleAsync(CancellationToken.None);

        var usd = Assert.Single(rates, x => x.Currency == "USD");
        Assert.Equal(5.40m, usd.MidRate);
        Assert.Equal(5.427m, usd.BuyRate);
        Assert.Equal(5.373m, usd.SellRate);
        Assert.Equal(time.GetUtcNow(), usd.QuotedAt);
        Assert.False(string.IsNullOrWhiteSpace(usd.ServedBy));
    }

    [Fact]
    public async Task GetRate_LowercaseCurrency_IsNormalized()
    {
        var handler = new GetRateHandler(baseRates, Calculator(0m), time);

        var rate = await handler.HandleAsync("eur", CancellationToken.None);

        Assert.Equal("EUR", rate.Currency);
    }

    [Fact]
    public async Task GetRate_UnknownCurrency_ThrowsResourceNotFound()
    {
        var handler = new GetRateHandler(baseRates, Calculator(0m), time);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => handler.HandleAsync("XYZ", CancellationToken.None));
    }

    private CreateQuoteHandler CreateHandler(decimal variation = 0m) =>
        new(baseRates, quotes, Calculator(variation), options, time);

    private LiveRateCalculator Calculator(decimal variation) => new(new FixedVariationSource(variation), options);
}
