using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Mappers.ExchangeRates;
using Presentation.Models.ExchangeRates;
using Presentation.Validators.Common;

namespace Presentation.Validators.ExchangeRates;

public class ListExchangeRateRequestValidator(
    IExchangeRateService exchangeRateService,
    TimeProvider timeProvider) : PagedRequestValidator<ListExchangeRateRequestDto>(timeProvider)
{
    protected sealed override void Initialize()
    {
        base.Initialize();

        RuleFor(x => x.FromCurrency).IsInEnum().When(x => x.FromCurrency.HasValue);
        RuleFor(x => x.ToCurrency).IsInEnum().When(x => x.ToCurrency.HasValue);
    }

    protected override Task<TotalCount> GetCountAsync(ListExchangeRateRequestDto request, CancellationToken stoppingToken)
    {
        return exchangeRateService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}
