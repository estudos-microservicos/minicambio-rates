using Microsoft.EntityFrameworkCore;
using MiniCambio.Rates.Api.Application;
using MiniCambio.Rates.Api.Domain;

namespace MiniCambio.Rates.Api.Infrastructure.Persistence;

internal sealed class BaseRateRepository(RatesDbContext db) : IBaseRateRepository
{
    public async Task<IReadOnlyList<BaseRate>> GetAllAsync(CancellationToken cancellationToken) =>
        await db.BaseRates.AsNoTracking().OrderBy(x => x.Currency).ToListAsync(cancellationToken);

    public Task<BaseRate?> GetAsync(string currency, CancellationToken cancellationToken) =>
        db.BaseRates.AsNoTracking().FirstOrDefaultAsync(x => x.Currency == currency, cancellationToken);
}

internal sealed class QuoteRepository(RatesDbContext db) : IQuoteRepository
{
    public async Task AddAsync(Quote quote, CancellationToken cancellationToken)
    {
        db.Quotes.Add(quote);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<Quote?> GetAsync(Guid id, CancellationToken cancellationToken) =>
        db.Quotes.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
}
