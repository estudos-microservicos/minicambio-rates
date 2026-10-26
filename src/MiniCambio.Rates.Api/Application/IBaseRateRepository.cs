using MiniCambio.Rates.Api.Domain;

namespace MiniCambio.Rates.Api.Application;

public interface IBaseRateRepository
{
    Task<IReadOnlyList<BaseRate>> GetAllAsync(CancellationToken cancellationToken);

    Task<BaseRate?> GetAsync(string currency, CancellationToken cancellationToken);
}
