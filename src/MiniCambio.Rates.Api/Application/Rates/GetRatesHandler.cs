namespace MiniCambio.Rates.Api.Application.Rates;

public sealed class GetRatesHandler(
    IBaseRateRepository baseRates,
    LiveRateCalculator calculator,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<RateResponse>> HandleAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var rates = await baseRates.GetAllAsync(cancellationToken);
        return [.. rates.Select(rate => calculator.ToResponse(rate, now))];
    }
}
