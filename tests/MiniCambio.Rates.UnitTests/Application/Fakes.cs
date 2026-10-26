using MiniCambio.Rates.Api.Application;
using MiniCambio.Rates.Api.Domain;

namespace MiniCambio.Rates.UnitTests.Application;

internal sealed class FixedVariationSource(decimal variation = 0m) : IRateVariationSource
{
    public decimal NextVariation() => variation;
}

internal sealed class InMemoryBaseRateRepository : IBaseRateRepository
{
    private readonly List<BaseRate> rates =
    [
        new("EUR", 5.85m, DateTimeOffset.UnixEpoch),
        new("USD", 5.40m, DateTimeOffset.UnixEpoch),
    ];

    public Task<IReadOnlyList<BaseRate>> GetAllAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<BaseRate>>(rates);

    public Task<BaseRate?> GetAsync(string currency, CancellationToken cancellationToken) =>
        Task.FromResult(rates.FirstOrDefault(x => x.Currency == currency));
}

internal sealed class InMemoryQuoteRepository : IQuoteRepository
{
    public List<Quote> Quotes { get; } = [];

    public Task AddAsync(Quote quote, CancellationToken cancellationToken)
    {
        Quotes.Add(quote);
        return Task.CompletedTask;
    }

    public Task<Quote?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Quotes.FirstOrDefault(x => x.Id == id));
}
