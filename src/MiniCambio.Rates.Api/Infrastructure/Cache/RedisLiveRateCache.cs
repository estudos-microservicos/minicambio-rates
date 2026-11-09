using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using MiniCambio.Rates.Api.Application.Rates;
using StackExchange.Redis;

namespace MiniCambio.Rates.Api.Infrastructure.Cache;

/// <summary>Connection string do Redis (<c>ConnectionStrings__Redis</c>), validada na subida.</summary>
public sealed class RedisOptions
{
    [Required(ErrorMessage = "Connection string 'Redis' não configurada (variável ConnectionStrings__Redis).")]
    public string? ConnectionString { get; set; }
}

/// <summary>
/// Guarda cada taxa ao vivo numa chave do Redis (<c>rates:live-mid:USD</c>) com prazo de expiração: quando o prazo
/// acaba, o próprio Redis apaga a chave e a próxima consulta gera uma taxa nova.
/// O valor é JSON legível, para dar para inspecionar com <c>redis-cli GET rates:live-mid:USD</c>.
/// </summary>
internal sealed class RedisLiveRateCache(IConnectionMultiplexer redis, ILogger<RedisLiveRateCache> logger)
    : ILiveRateCache
{
    public const string KeyPrefix = "rates:live-mid:";

    public async Task<LiveMidRate?> GetAsync(string currency, CancellationToken cancellationToken)
    {
        try
        {
            var value = await redis.GetDatabase().StringGetAsync(Key(currency));
            return value.IsNullOrEmpty ? null : Deserialize(value);
        }
        catch (Exception ex) when (IsCacheFailure(ex))
        {
            logger.LogWarning(ex, "Redis indisponível ao ler a taxa de {Currency}; seguindo sem cache", currency);
            return null;
        }
    }

    public async Task<LiveMidRate> AddIfAbsentAsync(
        string currency, LiveMidRate rate, TimeSpan ttl, CancellationToken cancellationToken)
    {
        try
        {
            var database = redis.GetDatabase();

            // SET ... NX EX: grava só se a chave não existir. É uma operação atômica no Redis, então se duas réplicas
            // gerarem taxas ao mesmo tempo, só a primeira grava; a segunda lê e passa a usar a da primeira.
            if (await database.StringSetAsync(Key(currency), JsonSerializer.Serialize(rate, JsonOptions), ttl, When.NotExists))
            {
                logger.LogInformation(
                    "Taxa ao vivo de {Currency} gerada e gravada no cache por {TtlSeconds} s", currency, ttl.TotalSeconds);
                return rate;
            }

            var current = await database.StringGetAsync(Key(currency));
            return current.IsNullOrEmpty ? rate : Deserialize(current); // vazio: expirou entre o SET e o GET
        }
        catch (Exception ex) when (IsCacheFailure(ex))
        {
            logger.LogWarning(ex, "Redis indisponível ao gravar a taxa de {Currency}; seguindo sem cache", currency);
            return rate;
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = JsonSerializerOptions.Web;

    private static RedisKey Key(string currency) => KeyPrefix + currency;

    private static LiveMidRate Deserialize(RedisValue value) =>
        JsonSerializer.Deserialize<LiveMidRate>(value.ToString(), JsonOptions)
        ?? throw new JsonException("Valor vazio no cache.");

    // RedisConnectionException (sem conexão) herda de RedisException; RedisTimeoutException herda de TimeoutException.
    // JsonException: valor ilegível no cache (ex.: gravado à mão). Em todos os casos, melhor seguir sem cache.
    private static bool IsCacheFailure(Exception ex) => ex is RedisException or TimeoutException or JsonException;
}
