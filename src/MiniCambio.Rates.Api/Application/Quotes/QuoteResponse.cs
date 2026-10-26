using MiniCambio.Rates.Api.Domain;

namespace MiniCambio.Rates.Api.Application.Quotes;

public sealed record QuoteResponse(
    Guid Id,
    string Currency,
    QuoteSide Side,
    decimal ForeignAmount,
    decimal Rate,
    decimal BrlAmount,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    bool IsExpired,
    string ServedBy)
{
    public static QuoteResponse From(Quote quote, DateTimeOffset now) => new(
        quote.Id,
        quote.Currency,
        quote.Side,
        quote.ForeignAmount,
        quote.Rate,
        quote.BrlAmount,
        quote.CreatedAt,
        quote.ExpiresAt,
        quote.IsExpired(now),
        Environment.MachineName);
}
