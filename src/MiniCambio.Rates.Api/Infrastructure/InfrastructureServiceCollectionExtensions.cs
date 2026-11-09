using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MiniCambio.Rates.Api.Application;
using MiniCambio.Rates.Api.Application.Rates;
using MiniCambio.Rates.Api.Infrastructure.Cache;
using MiniCambio.Rates.Api.Infrastructure.Persistence;
using StackExchange.Redis;

namespace MiniCambio.Rates.Api.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public const string ReadyTag = "ready";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddOptions<DatabaseOptions>()
            .BindConfiguration(DatabaseOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddDbContext<RatesDbContext>((serviceProvider, options) =>
        {
            var connectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString("Default");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string 'Default' não configurada (variável ConnectionStrings__Default).");
            }

            options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
        });

        services.AddScoped<IBaseRateRepository, BaseRateRepository>();
        services.AddScoped<IQuoteRepository, QuoteRepository>();
        services.AddSingleton<IRateVariationSource, RandomRateVariationSource>();

        AddRedisCache(services);

        // Live não tem dependências (só "o processo responde"); ready inclui o banco.
        // O Redis fica de fora de propósito: sem ele a API continua respondendo, só que sem cache.
        services.AddHealthChecks().AddDbContextCheck<RatesDbContext>("database", tags: [ReadyTag]);

        return services;
    }

    private static void AddRedisCache(IServiceCollection services)
    {
        services.AddOptions<RedisOptions>()
            .Configure<IConfiguration>((options, configuration) =>
                options.ConnectionString = configuration.GetConnectionString("Redis"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Uma conexão só por processo, compartilhada (o ConnectionMultiplexer é thread-safe e foi feito para isso).
        services.AddSingleton<IConnectionMultiplexer>(serviceProvider =>
        {
            var options = ConfigurationOptions.Parse(
                serviceProvider.GetRequiredService<IOptions<RedisOptions>>().Value.ConnectionString!);

            options.AbortOnConnectFail = false; // sobe mesmo com o Redis fora do ar e fica tentando reconectar
            options.BacklogPolicy = BacklogPolicy.FailFast; // sem conexão, falha na hora em vez de enfileirar
            options.ConnectTimeout = 2000;
            options.SyncTimeout = 1000;
            options.AsyncTimeout = 1000; // cache lento não pode deixar a API lenta: desiste em 1 s e segue sem ele
            return ConnectionMultiplexer.Connect(options);
        });
        services.AddSingleton<ILiveRateCache, RedisLiveRateCache>();
    }
}
