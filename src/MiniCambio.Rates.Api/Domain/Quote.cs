namespace MiniCambio.Rates.Api.Domain;

/// <summary>Cotação emitida para um cliente, válida até <see cref="ExpiresAt"/>.</summary>
public sealed class Quote
{
    // Construtor usado pelo EF Core.
    private Quote()
    {
        Currency = string.Empty;
    }

    private Quote(
        Guid id,
        string currency,
        QuoteSide side,
        decimal foreignAmount,
        decimal rate,
        decimal brlAmount,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
    {
        Id = id;
        Currency = currency;
        Side = side;
        ForeignAmount = foreignAmount;
        Rate = rate;
        BrlAmount = brlAmount;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public Guid Id { get; private set; }

    public string Currency { get; private set; }

    public QuoteSide Side { get; private set; }

    public decimal ForeignAmount { get; private set; }

    public decimal Rate { get; private set; }

    public decimal BrlAmount { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public static Quote Create(
        string currency,
        QuoteSide side,
        decimal foreignAmount,
        decimal rate,
        DateTimeOffset now,
        TimeSpan timeToLive)
    {
        if (foreignAmount <= 0)
        {
            throw new DomainException("O valor em moeda estrangeira deve ser maior que zero.");
        }

        if (timeToLive <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeToLive), timeToLive, "A validade deve ser positiva.");
        }

        return new Quote(
            Guid.CreateVersion7(now),
            currency,
            side,
            foreignAmount,
            rate,
            RatePricing.CalculateBrlAmount(foreignAmount, rate),
            now,
            now.Add(timeToLive));
    }

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;
}
