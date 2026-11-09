namespace MiniCambio.Rates.Api.Application.Rates;

/// <summary>Taxa média "ao vivo" de uma moeda e o instante em que ela foi gerada.</summary>
public sealed record LiveMidRate(decimal MidRate, DateTimeOffset QuotedAt);

/// <summary>
/// Cache das taxas ao vivo, compartilhado por todas as réplicas da API (Redis). Enquanto a taxa de uma moeda não
/// expira, toda consulta e toda cotação usam o mesmo valor, em qualquer réplica.
/// Falhas do cache não derrubam a API: a implementação registra o erro e se comporta como um cache vazio.
/// </summary>
public interface ILiveRateCache
{
    /// <summary>A taxa guardada para a moeda, ou <c>null</c> se não houver (ou se o cache estiver fora do ar).</summary>
    Task<LiveMidRate?> GetAsync(string currency, CancellationToken cancellationToken);

    /// <summary>
    /// Grava a taxa só se ainda não houver uma para a moeda, e devolve a que ficou valendo: a recebida, ou a que
    /// outra réplica gravou primeiro. Assim duas réplicas que geram taxas ao mesmo tempo acabam usando a mesma.
    /// </summary>
    Task<LiveMidRate> AddIfAbsentAsync(string currency, LiveMidRate rate, TimeSpan ttl, CancellationToken cancellationToken);
}
