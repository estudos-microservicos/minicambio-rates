namespace MiniCambio.Rates.Api.Application.Rates;

public sealed class GetRatesHandler(IBaseRateRepository baseRates, LiveRateCalculator calculator)
{
    public async Task<IReadOnlyList<RateResponse>> HandleAsync(CancellationToken cancellationToken)
    {
        var rates = await baseRates.GetAllAsync(cancellationToken);

        // Uma ida ao cache por moeda, em sequência: são só quatro moedas.
        var responses = new List<RateResponse>(rates.Count);
        foreach (var rate in rates)
        {
            responses.Add(await calculator.ToResponseAsync(rate, cancellationToken));
        }

        return responses;
    }
}
