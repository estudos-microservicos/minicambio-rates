using Microsoft.Extensions.Options;
using MiniCambio.Rates.Api.Application.Rates;
using MiniCambio.Rates.Api.Domain;

namespace MiniCambio.Rates.Api.Application.Quotes;

public sealed class CreateQuoteHandler(
    IBaseRateRepository baseRates,
    IQuoteRepository quotes,
    LiveRateCalculator calculator,
    IOptions<RatesOptions> options,
    TimeProvider timeProvider)
{
    /// <summary>Emite uma cotação. Espera um request já validado.</summary>
    public async Task<QuoteResponse> HandleAsync(CreateQuoteRequest request, CancellationToken cancellationToken)
    {
        var currency = request.Currency!;
        var side = Enum.Parse<QuoteSide>(request.Side!, ignoreCase: true);
        var foreignAmount = request.ForeignAmount!.Value;

        var baseRate = await baseRates.GetAsync(currency, cancellationToken)
            ?? throw new UnsupportedCurrencyException(currency);

        var now = timeProvider.GetUtcNow();
        var rate = calculator.GetRate(calculator.GetLiveMidRate(baseRate), side);
        var quote = Quote.Create(
            currency,
            side,
            foreignAmount,
            rate,
            now,
            TimeSpan.FromSeconds(options.Value.QuoteTtlSeconds));

        await quotes.AddAsync(quote, cancellationToken);

        return QuoteResponse.From(quote, now);
    }
}
