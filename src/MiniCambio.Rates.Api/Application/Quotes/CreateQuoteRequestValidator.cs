using FluentValidation;
using MiniCambio.Rates.Api.Domain;

namespace MiniCambio.Rates.Api.Application.Quotes;

public sealed class CreateQuoteRequestValidator : AbstractValidator<CreateQuoteRequest>
{
    public CreateQuoteRequestValidator()
    {
        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Informe a moeda.")
            .Matches("^[A-Z]{3}$").WithMessage("A moeda deve ser um código ISO 4217 com 3 letras maiúsculas (ex.: USD).");

        RuleFor(x => x.Side)
            .NotEmpty().WithMessage("Informe a operação.")
            .IsEnumName(typeof(QuoteSide), caseSensitive: false).WithMessage("A operação deve ser Buy ou Sell.");

        RuleFor(x => x.ForeignAmount)
            .NotNull().WithMessage("Informe o valor.")
            .GreaterThan(0).WithMessage("O valor deve ser maior que zero.")
            .PrecisionScale(18, 2, ignoreTrailingZeros: true).WithMessage("O valor deve ter no máximo 2 casas decimais.");
    }
}
