using MiniCambio.Rates.Api.Domain;

namespace MiniCambio.Rates.UnitTests.Domain;

public class RatePricingTests
{
    [Fact]
    public void ApplySpread_Buy_AddsSpreadToMidRate()
    {
        var rate = RatePricing.ApplySpread(5.40m, QuoteSide.Buy, spreadPercent: 0.5m);

        Assert.Equal(5.427m, rate);
    }

    [Fact]
    public void ApplySpread_Sell_SubtractsSpreadFromMidRate()
    {
        var rate = RatePricing.ApplySpread(5.40m, QuoteSide.Sell, spreadPercent: 0.5m);

        Assert.Equal(5.373m, rate);
    }

    [Fact]
    public void ApplySpread_ResultWithMoreThanSixDecimals_RoundsToSix()
    {
        var rate = RatePricing.ApplySpread(5.123457m, QuoteSide.Buy, spreadPercent: 0.5m);

        // 5.123457 * 1.005 = 5.149074285 -> 5.149074
        Assert.Equal(5.149074m, rate);
    }

    [Theory]
    [InlineData(0, 5.40)]
    [InlineData(0.01, 5.454)]
    [InlineData(-0.01, 5.346)]
    public void ApplyVariation_WithinLimit_AppliesVariation(decimal variation, decimal expected)
    {
        Assert.Equal(expected, RatePricing.ApplyVariation(5.40m, variation));
    }

    [Theory]
    [InlineData(0.0101)]
    [InlineData(-0.0101)]
    public void ApplyVariation_OutOfLimit_Throws(decimal variation)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RatePricing.ApplyVariation(5.40m, variation));
    }

    [Theory]
    [InlineData(100, 5.427, 542.70)]
    [InlineData(0.01, 5.425, 0.05)] // 0,05425 -> 0,05
    [InlineData(1, 0.125, 0.13)] // ponto médio arredonda para longe do zero
    public void CalculateBrlAmount_RoundsToTwoDecimalsAwayFromZero(decimal foreignAmount, decimal rate, decimal expected)
    {
        Assert.Equal(expected, RatePricing.CalculateBrlAmount(foreignAmount, rate));
    }
}
