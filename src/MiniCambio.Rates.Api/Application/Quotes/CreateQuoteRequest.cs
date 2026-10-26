namespace MiniCambio.Rates.Api.Application.Quotes;

/// <summary>
/// Corpo do <c>POST /api/v1/quotes</c>. Os campos são anuláveis e o <c>side</c> é texto para que a validação
/// devolva mensagens por campo em vez de um erro genérico de desserialização.
/// </summary>
public sealed record CreateQuoteRequest(string? Currency, string? Side, decimal? ForeignAmount);
