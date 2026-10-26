using MiniCambio.Rates.Api.Domain;

namespace MiniCambio.Rates.Api.Application;

public interface IQuoteRepository
{
    Task AddAsync(Quote quote, CancellationToken cancellationToken);

    Task<Quote?> GetAsync(Guid id, CancellationToken cancellationToken);
}
