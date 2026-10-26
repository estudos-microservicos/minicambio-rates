namespace MiniCambio.Rates.Api.Domain;

/// <summary>Cálculos puros de taxas e valores (sem estado, sem I/O).</summary>
public static class RatePricing
{
    public const int RateDecimals = 6;
    public const int MoneyDecimals = 2;

    /// <summary>Variação máxima da taxa "ao vivo" em relação à taxa média da tabela (±1%).</summary>
    public const decimal MaxVariation = 0.01m;

    /// <summary>Aplica a variação (entre -1% e +1%) sobre a taxa média.</summary>
    public static decimal ApplyVariation(decimal midRate, decimal variation)
    {
        if (variation is < -MaxVariation or > MaxVariation)
        {
            throw new ArgumentOutOfRangeException(nameof(variation), variation, "A variação deve estar entre -1% e +1%.");
        }

        return RoundRate(midRate * (1 + variation));
    }

    /// <summary>
    /// Taxa cobrada do cliente: na compra o spread soma, na venda subtrai.
    /// <paramref name="spreadPercent"/> é em pontos percentuais (0.5 = 0,5%).
    /// </summary>
    public static decimal ApplySpread(decimal midRate, QuoteSide side, decimal spreadPercent)
    {
        var spread = spreadPercent / 100m;
        var rate = side switch
        {
            QuoteSide.Buy => midRate * (1 + spread),
            QuoteSide.Sell => midRate * (1 - spread),
            _ => throw new ArgumentOutOfRangeException(nameof(side), side, null),
        };

        return RoundRate(rate);
    }

    public static decimal CalculateBrlAmount(decimal foreignAmount, decimal rate) =>
        Math.Round(foreignAmount * rate, MoneyDecimals, MidpointRounding.AwayFromZero);

    private static decimal RoundRate(decimal rate) =>
        Math.Round(rate, RateDecimals, MidpointRounding.AwayFromZero);
}
