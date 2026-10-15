using Core.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.ExchangeRates;

public record CreateExchangeRateRequestDto
{
    [FromBody]
    public required CreateExchangeRateRequestBodyDto Body { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid? CreatedByUserId { get; set; }
}

public record CreateExchangeRateRequestBodyDto
{
    public required Currency FromCurrency { get; init; }
    public required Currency ToCurrency { get; init; }

    public required decimal Rate { get; init; }

    public DateTimeOffset? EffectiveFrom { get; init; }
}
