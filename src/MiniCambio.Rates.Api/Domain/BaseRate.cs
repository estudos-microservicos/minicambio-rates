namespace MiniCambio.Rates.Api.Domain;

/// <summary>Taxa média de referência de uma moeda, em BRL por unidade da moeda estrangeira.</summary>
public sealed class BaseRate
{
    public BaseRate(string currency, decimal midRate, DateTimeOffset updatedAt)
    {
        Currency = currency;
        MidRate = midRate;
        UpdatedAt = updatedAt;
    }

    public string Currency { get; private set; }

    public decimal MidRate { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }
}
