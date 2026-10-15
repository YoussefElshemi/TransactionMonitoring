using FluentValidation;
using Presentation.Models.ExchangeRates;

namespace Presentation.Validators.ExchangeRates;

public class CreateExchangeRateRequestValidator : AbstractValidator<CreateExchangeRateRequestDto>
{
    public CreateExchangeRateRequestValidator()
    {
        RuleFor(x => x.Body.FromCurrency).IsInEnum();
        RuleFor(x => x.Body.ToCurrency).IsInEnum();

        RuleFor(x => x.Body.ToCurrency)
            .NotEqual(x => x.Body.FromCurrency)
            .WithMessage("A currency does not need a rate against itself");

        RuleFor(x => x.Body.Rate)
            .GreaterThan(0)
            .WithMessage("A rate of nothing would price every converted line at nothing");
    }
}
