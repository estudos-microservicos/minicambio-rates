using Microsoft.EntityFrameworkCore;
using MiniCambio.Rates.Api.Application;
using MiniCambio.Rates.Api.Infrastructure.Persistence;

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

        // Live não tem dependências (só "o processo responde"); ready inclui o banco.
        services.AddHealthChecks().AddDbContextCheck<RatesDbContext>("database", tags: [ReadyTag]);

        return services;
    }
}
