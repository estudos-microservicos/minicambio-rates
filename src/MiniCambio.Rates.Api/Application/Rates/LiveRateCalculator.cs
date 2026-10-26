using Microsoft.Extensions.Options;
using MiniCambio.Rates.Api.Domain;

namespace MiniCambio.Rates.Api.Application.Rates;

/// <summary>Calcula a taxa "ao vivo" (com variação aleatória, não persistida) a partir da taxa da tabela.</summary>
public sealed class LiveRateCalculator(IRateVariationSource variationSource, IOptions<RatesOptions> options)
{
    public decimal GetLiveMidRate(BaseRate baseRate) =>
        RatePricing.ApplyVariation(baseRate.MidRate, variationSource.NextVariation());

    public decimal GetRate(decimal liveMidRate, QuoteSide side) =>
        RatePricing.ApplySpread(liveMidRate, side, options.Value.SpreadPercent);

    public RateResponse ToResponse(BaseRate baseRate, DateTimeOffset now)
    {
        var mid = GetLiveMidRate(baseRate);
        return new RateResponse(
            baseRate.Currency,
            mid,
            GetRate(mid, QuoteSide.Buy),
            GetRate(mid, QuoteSide.Sell),
            now,
            Environment.MachineName);
    }
}
