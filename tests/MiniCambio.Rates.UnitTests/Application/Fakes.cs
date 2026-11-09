using MiniCambio.Rates.Api.Application;
using MiniCambio.Rates.Api.Application.Rates;
using MiniCambio.Rates.Api.Domain;

namespace MiniCambio.Rates.UnitTests.Application;

internal sealed class FixedVariationSource(decimal variation = 0m) : IRateVariationSource
{
    public decimal NextVariation() => variation;
}

/// <summary>Devolve as variações na ordem dada, para provar quando uma taxa nova foi (ou não) gerada.</summary>
internal sealed class SequenceVariationSource(params decimal[] variations) : IRateVariationSource
{
    private int next;

    public int Calls => next;

    public decimal NextVariation() => variations[next++];
}

/// <summary>Cache em memória que respeita o prazo pelo relógio do teste (FakeTimeProvider).</summary>
internal sealed class InMemoryLiveRateCache(TimeProvider time) : ILiveRateCache
{
    private readonly Dictionary<string, (LiveMidRate Rate, DateTimeOffset ExpiresAt)> entries = [];

    public TimeSpan? LastTtl { get; private set; }

    public Task<LiveMidRate?> GetAsync(string currency, CancellationToken cancellationToken) =>
        Task.FromResult(entries.TryGetValue(currency, out var entry) && entry.ExpiresAt > time.GetUtcNow()
            ? entry.Rate
            : null);

    public async Task<LiveMidRate> AddIfAbsentAsync(
        string currency, LiveMidRate rate, TimeSpan ttl, CancellationToken cancellationToken)
    {
        LastTtl = ttl;
        if (await GetAsync(currency, cancellationToken) is { } current)
        {
            return current;
        }

        entries[currency] = (rate, time.GetUtcNow() + ttl);
        return rate;
    }

    /// <summary>Simula outra réplica gravando primeiro.</summary>
    public void Seed(string currency, LiveMidRate rate, TimeSpan ttl) =>
        entries[currency] = (rate, time.GetUtcNow() + ttl);
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
