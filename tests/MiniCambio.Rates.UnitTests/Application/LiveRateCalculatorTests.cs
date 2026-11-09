using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using MiniCambio.Rates.Api.Application;
using MiniCambio.Rates.Api.Application.Quotes;
using MiniCambio.Rates.Api.Application.Rates;
using MiniCambio.Rates.Api.Domain;

namespace MiniCambio.Rates.UnitTests.Application;

public class LiveRateCalculatorTests
{
    private static readonly BaseRate Usd = new("USD", 5.40m, DateTimeOffset.UnixEpoch);

    private readonly FakeTimeProvider time = new(new DateTimeOffset(2026, 11, 9, 12, 0, 0, TimeSpan.Zero));
    private readonly IOptions<RatesOptions> options =
        Options.Create(new RatesOptions { SpreadPercent = 0.5m, QuoteTtlSeconds = 60, LiveRateCacheSeconds = 300 });

    [Fact]
    public async Task GetLiveMidRate_WithinCacheTtl_ReusesCachedRate()
    {
        var variations = new SequenceVariationSource(0.01m, -0.01m);
        var calculator = new LiveRateCalculator(variations, new InMemoryLiveRateCache(time), options, time);

        var first = await calculator.GetLiveMidRateAsync(Usd, CancellationToken.None);
        time.Advance(TimeSpan.FromSeconds(299));
        var second = await calculator.GetLiveMidRateAsync(Usd, CancellationToken.None);

        Assert.Equal(5.454m, first.MidRate); // 5,40 * 1,01
        Assert.Equal(first, second);
        Assert.Equal(1, variations.Calls);
    }

    [Fact]
    public async Task GetLiveMidRate_AfterCacheTtl_GeneratesNewRate()
    {
        var variations = new SequenceVariationSource(0.01m, -0.01m);
        var calculator = new LiveRateCalculator(variations, new InMemoryLiveRateCache(time), options, time);

        await calculator.GetLiveMidRateAsync(Usd, CancellationToken.None);
        time.Advance(TimeSpan.FromSeconds(300));
        var renewed = await calculator.GetLiveMidRateAsync(Usd, CancellationToken.None);

        Assert.Equal(5.346m, renewed.MidRate); // 5,40 * 0,99
        Assert.Equal(time.GetUtcNow(), renewed.QuotedAt);
        Assert.Equal(2, variations.Calls);
    }

    [Fact]
    public async Task GetLiveMidRate_CacheMiss_StoresWithConfiguredTtl()
    {
        var cache = new InMemoryLiveRateCache(time);
        var calculator = new LiveRateCalculator(new FixedVariationSource(), cache, options, time);

        await calculator.GetLiveMidRateAsync(Usd, CancellationToken.None);

        Assert.Equal(TimeSpan.FromSeconds(300), cache.LastTtl);
    }

    [Fact]
    public async Task ToResponse_CachedRate_ReturnsWhenTheRateWasGenerated()
    {
        var calculator = new LiveRateCalculator(new FixedVariationSource(), new InMemoryLiveRateCache(time), options, time);
        var generatedAt = time.GetUtcNow();

        await calculator.GetLiveMidRateAsync(Usd, CancellationToken.None);
        time.Advance(TimeSpan.FromSeconds(42));
        var response = await calculator.ToResponseAsync(Usd, CancellationToken.None);

        Assert.Equal(generatedAt, response.QuotedAt);
        Assert.Equal(5.427m, response.BuyRate);
        Assert.Equal(5.373m, response.SellRate);
    }

    [Fact]
    public async Task CreateQuote_RateAlreadyCachedByAnotherReplica_UsesCachedRate()
    {
        var cache = new InMemoryLiveRateCache(time);
        cache.Seed("USD", new LiveMidRate(5.50m, time.GetUtcNow()), TimeSpan.FromSeconds(300));
        var variations = new SequenceVariationSource();
        var handler = new CreateQuoteHandler(
            new InMemoryBaseRateRepository(),
            new InMemoryQuoteRepository(),
            new LiveRateCalculator(variations, cache, options, time),
            options,
            time);

        var quote = await handler.HandleAsync(new CreateQuoteRequest("USD", "Buy", 100m), CancellationToken.None);

        Assert.Equal(5.5275m, quote.Rate); // 5,50 * 1,005
        Assert.Equal(0, variations.Calls);
    }
}
