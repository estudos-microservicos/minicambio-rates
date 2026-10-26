using MiniCambio.Rates.Api.Application.Quotes;

namespace MiniCambio.Rates.Api.Endpoints;

public static class QuotesEndpoints
{
    public static IEndpointRouteBuilder MapQuotesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/quotes").WithTags("Quotes");

        group.MapPost("/", async (CreateQuoteRequest request, CreateQuoteHandler handler, CancellationToken cancellationToken) =>
            {
                var quote = await handler.HandleAsync(request, cancellationToken);
                return TypedResults.Created($"/api/v1/quotes/{quote.Id}", quote);
            })
            .AddEndpointFilter<ValidationFilter<CreateQuoteRequest>>()
            .WithName("CreateQuote")
            .WithSummary("Emite uma cotação com prazo de validade")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/{id:guid}", async (Guid id, GetQuoteHandler handler, CancellationToken cancellationToken) =>
                TypedResults.Ok(await handler.HandleAsync(id, cancellationToken)))
            .WithName("GetQuote")
            .WithSummary("Consulta uma cotação")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
