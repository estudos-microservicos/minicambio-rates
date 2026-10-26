using MiniCambio.Rates.Api.Application.Quotes;

namespace MiniCambio.Rates.UnitTests.Application;

public class CreateQuoteRequestValidatorTests
{
    private readonly CreateQuoteRequestValidator validator = new();

    [Fact]
    public void Validate_ValidRequest_HasNoErrors()
    {
        var result = validator.Validate(new CreateQuoteRequest("USD", "Buy", 1500.50m));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("usd")]
    [InlineData("US")]
    [InlineData("USDT")]
    public void Validate_InvalidCurrency_HasCurrencyError(string? currency)
    {
        var result = validator.Validate(new CreateQuoteRequest(currency, "Buy", 10m));

        Assert.Contains(result.Errors, e => string.Equals(e.PropertyName, "currency", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Hold")]
    public void Validate_InvalidSide_HasSideError(string? side)
    {
        var result = validator.Validate(new CreateQuoteRequest("USD", side, 10m));

        Assert.Contains(result.Errors, e => string.Equals(e.PropertyName, "side", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0.0)]
    [InlineData(-5.0)]
    [InlineData(10.123)]
    public void Validate_InvalidAmount_HasForeignAmountError(double? amount)
    {
        var result = validator.Validate(new CreateQuoteRequest("USD", "Sell", (decimal?)amount));

        Assert.Contains(result.Errors, e => string.Equals(e.PropertyName, "foreignAmount", StringComparison.OrdinalIgnoreCase));
    }
}
