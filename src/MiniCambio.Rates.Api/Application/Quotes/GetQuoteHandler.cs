namespace MiniCambio.Rates.Api.Application.Quotes;

public sealed class GetQuoteHandler(IQuoteRepository quotes, TimeProvider timeProvider)
{
    public async Task<QuoteResponse> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var quote = await quotes.GetAsync(id, cancellationToken)
            ?? throw new ResourceNotFoundException("Cotação não encontrada.");

        return QuoteResponse.From(quote, timeProvider.GetUtcNow());
    }
}
