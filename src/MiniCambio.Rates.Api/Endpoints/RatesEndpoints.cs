using MiniCambio.Rates.Api.Application.Rates;

namespace MiniCambio.Rates.Api.Endpoints;

public static class RatesEndpoints
{
    public static IEndpointRouteBuilder MapRatesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/rates").WithTags("Rates");

        group.MapGet("/", async (GetRatesHandler handler, CancellationToken cancellationToken) =>
                TypedResults.Ok(await handler.HandleAsync(cancellationToken)))
            .WithName("ListRates")
            .WithSummary("Lista as taxas de todas as moedas suportadas");

        group.MapGet("/{currency}", async (string currency, GetRateHandler handler, CancellationToken cancellationToken) =>
                TypedResults.Ok(await handler.HandleAsync(currency, cancellationToken)))
            .WithName("GetRate")
            .WithSummary("Consulta a taxa de uma moeda")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
