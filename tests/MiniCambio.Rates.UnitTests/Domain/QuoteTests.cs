using MiniCambio.Rates.Api.Domain;

namespace MiniCambio.Rates.UnitTests.Domain;

public class QuoteTests
{
    private static readonly DateTimeOffset Now = new(2026, 5, 10, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_ValidData_CalculatesBrlAmountAndExpiration()
    {
        var quote = Quote.Create("USD", QuoteSide.Buy, 100m, 5.427m, Now, TimeSpan.FromSeconds(60));

        Assert.NotEqual(Guid.Empty, quote.Id);
        Assert.Equal("USD", quote.Currency);
        Assert.Equal(QuoteSide.Buy, quote.Side);
        Assert.Equal(542.70m, quote.BrlAmount);
        Assert.Equal(Now, quote.CreatedAt);
        Assert.Equal(Now.AddSeconds(60), quote.ExpiresAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_NonPositiveAmount_ThrowsDomainException(decimal foreignAmount)
    {
        Assert.Throws<DomainException>(
            () => Quote.Create("USD", QuoteSide.Buy, foreignAmount, 5.4m, Now, TimeSpan.FromSeconds(60)));
    }

    [Fact]
    public void IsExpired_BeforeExpiration_ReturnsFalse()
    {
        var quote = Quote.Create("USD", QuoteSide.Sell, 10m, 5.4m, Now, TimeSpan.FromSeconds(60));

        Assert.False(quote.IsExpired(Now.AddSeconds(59)));
    }

    [Fact]
    public void IsExpired_AtOrAfterExpiration_ReturnsTrue()
    {
        var quote = Quote.Create("USD", QuoteSide.Sell, 10m, 5.4m, Now, TimeSpan.FromSeconds(60));

        Assert.True(quote.IsExpired(Now.AddSeconds(60)));
        Assert.True(quote.IsExpired(Now.AddMinutes(5)));
    }
}
