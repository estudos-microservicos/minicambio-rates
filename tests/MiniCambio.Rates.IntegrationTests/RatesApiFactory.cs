using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MiniCambio.Rates.Api.Application;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(MiniCambio.Rates.IntegrationTests.RatesApiFactory))]

namespace MiniCambio.Rates.IntegrationTests;

/// <summary>
/// Sobe a API contra um Postgres real (Testcontainers), compartilhado por todos os testes do assembly.
/// A variação aleatória da taxa é zerada para que os valores sejam determinísticos.
/// </summary>
public sealed class RatesApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer database = new PostgreSqlBuilder("postgres:17-alpine").Build();

    public async ValueTask InitializeAsync() => await database.StartAsync();

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await database.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Default", database.GetConnectionString());
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "true");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IRateVariationSource>();
            services.AddSingleton<IRateVariationSource>(new FixedVariationSource(0m));
        });
    }

    private sealed class FixedVariationSource(decimal variation) : IRateVariationSource
    {
        public decimal NextVariation() => variation;
    }
}
