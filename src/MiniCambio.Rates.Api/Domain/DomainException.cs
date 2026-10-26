namespace MiniCambio.Rates.Api.Domain;

/// <summary>Violação de regra de negócio. Vira 422 na API, com <see cref="Exception.Message"/> como detalhe.</summary>
public class DomainException(string message) : Exception(message);

public sealed class UnsupportedCurrencyException(string currency)
    : DomainException($"A moeda {currency} não é suportada.")
{
    public string Currency { get; } = currency;
}
