using Microsoft.EntityFrameworkCore;
using MiniCambio.Rates.Api.Domain;

namespace MiniCambio.Rates.Api.Infrastructure.Persistence;

public sealed class RatesDbContext(DbContextOptions<RatesDbContext> options) : DbContext(options)
{
    public DbSet<BaseRate> BaseRates => Set<BaseRate>();

    public DbSet<Quote> Quotes => Set<Quote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RatesDbContext).Assembly);
}
