using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MiniCambio.Rates.Api.Domain;

namespace MiniCambio.Rates.Api.Infrastructure.Persistence.Configurations;

internal sealed class QuoteConfiguration : IEntityTypeConfiguration<Quote>
{
    public void Configure(EntityTypeBuilder<Quote> builder)
    {
        builder.ToTable("quotes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Currency).HasMaxLength(3);
        builder.Property(x => x.Side).HasConversion<string>().HasMaxLength(4);
        builder.Property(x => x.ForeignAmount).HasPrecision(18, 2);
        builder.Property(x => x.Rate).HasPrecision(18, 6);
        builder.Property(x => x.BrlAmount).HasPrecision(18, 2);
    }
}
