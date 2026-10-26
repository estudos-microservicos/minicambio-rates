namespace MiniCambio.Rates.Api.Application.Rates;

public sealed record RateResponse(
    string Currency,
    decimal MidRate,
    decimal BuyRate,
    decimal SellRate,
    DateTimeOffset QuotedAt,
    string ServedBy);
