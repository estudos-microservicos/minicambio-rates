namespace MiniCambio.Rates.Api.Application.Rates;

public sealed class GetRateHandler(IBaseRateRepository baseRates, LiveRateCalculator calculator)
{
    public async Task<RateResponse> HandleAsync(string currency, CancellationToken cancellationToken)
    {
        var normalized = currency.Trim().ToUpperInvariant();
        var rate = await baseRates.GetAsync(normalized, cancellationToken)
            ?? throw new ResourceNotFoundException($"Não existe taxa cadastrada para a moeda {normalized}.");

        return await calculator.ToResponseAsync(rate, cancellationToken);
    }
}
