using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniCambio.Rates.Api.Domain;

namespace MiniCambio.Rates.Api.Infrastructure.Persistence.Configurations;

internal sealed class BaseRateConfiguration : IEntityTypeConfiguration<BaseRate>
{
    private static readonly DateTimeOffset SeedDate = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public void Configure(EntityTypeBuilder<BaseRate> builder)
    {
        builder.ToTable("base_rates");
        builder.HasKey(x => x.Currency);

        builder.Property(x => x.Currency).HasMaxLength(3);
        builder.Property(x => x.MidRate).HasPrecision(18, 6);

        // Seed versionado na migration inicial.
        builder.HasData(
            new BaseRate("USD", 5.40m, SeedDate),
            new BaseRate("EUR", 5.85m, SeedDate),
            new BaseRate("GBP", 6.90m, SeedDate),
            new BaseRate("CHF", 6.10m, SeedDate));
    }
}
