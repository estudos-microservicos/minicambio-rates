using MiniCambio.Rates.Api.Application;
using MiniCambio.Rates.Api.Domain;

namespace MiniCambio.Rates.Api.Infrastructure;

internal sealed class RandomRateVariationSource : IRateVariationSource
{
    public decimal NextVariation()
    {
        // NextDouble() está em [0, 1); convertido para [-1%, +1%).
        var variation = (decimal)((Random.Shared.NextDouble() * 2) - 1) * RatePricing.MaxVariation;
        return Math.Round(variation, 6, MidpointRounding.AwayFromZero);
    }
}
