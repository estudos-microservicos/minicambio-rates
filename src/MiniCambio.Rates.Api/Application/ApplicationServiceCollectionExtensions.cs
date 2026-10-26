using System.Text.Json;
using FluentValidation;
using FluentValidation.Internal;
using MiniCambio.Rates.Api.Application.Quotes;
using MiniCambio.Rates.Api.Application.Rates;

namespace MiniCambio.Rates.Api.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddOptions<RatesOptions>()
            .BindConfiguration(RatesOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Nomes de campo nos erros seguem o JSON (camelCase), para o cliente mapear cada erro ao seu campo.
        ValidatorOptions.Global.PropertyNameResolver = (_, member, expression) =>
        {
            var path = expression is null ? member?.Name : PropertyChain.FromExpression(expression).ToString();
            return path is null ? null : string.Join('.', path.Split('.').Select(JsonNamingPolicy.CamelCase.ConvertName));
        };
        services.AddValidatorsFromAssemblyContaining<CreateQuoteRequestValidator>();

        services.AddScoped<LiveRateCalculator>();
        services.AddScoped<GetRatesHandler>();
        services.AddScoped<GetRateHandler>();
        services.AddScoped<CreateQuoteHandler>();
        services.AddScoped<GetQuoteHandler>();

        return services;
    }
}
