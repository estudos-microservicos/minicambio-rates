using Microsoft.Extensions.Options;
using MiniCambio.Rates.Api.Domain;

namespace MiniCambio.Rates.Api.Application.Rates;

/// <summary>
/// Calcula a taxa "ao vivo" (com variação aleatória, não persistida) a partir da taxa da tabela.
/// A taxa gerada fica no cache por <see cref="RatesOptions.LiveRateCacheSeconds"/>; até expirar, todas as consultas
/// e cotações usam a mesma.
/// </summary>
public sealed class LiveRateCalculator(
    IRateVariationSource variationSource,
    ILiveRateCache cache,
    IOptions<RatesOptions> options,
    TimeProvider timeProvider)
{
    /// <summary>A taxa média ao vivo da moeda: a do cache, se houver; senão gera uma nova e grava no cache.</summary>
    public async Task<LiveMidRate> GetLiveMidRateAsync(BaseRate baseRate, CancellationToken cancellationToken)
    {
        var cached = await cache.GetAsync(baseRate.Currency, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var generated = new LiveMidRate(
            RatePricing.ApplyVariation(baseRate.MidRate, variationSource.NextVariation()),
            timeProvider.GetUtcNow());

        return await cache.AddIfAbsentAsync(
            baseRate.Currency,
            generated,
            TimeSpan.FromSeconds(options.Value.LiveRateCacheSeconds),
            cancellationToken);
    }

    public decimal GetRate(decimal liveMidRate, QuoteSide side) =>
        RatePricing.ApplySpread(liveMidRate, side, options.Value.SpreadPercent);

    public async Task<RateResponse> ToResponseAsync(BaseRate baseRate, CancellationToken cancellationToken)
    {
        var live = await GetLiveMidRateAsync(baseRate, cancellationToken);
        return new RateResponse(
            baseRate.Currency,
            live.MidRate,
            GetRate(live.MidRate, QuoteSide.Buy),
            GetRate(live.MidRate, QuoteSide.Sell),
            live.QuotedAt,
            Environment.MachineName);
    }
}
