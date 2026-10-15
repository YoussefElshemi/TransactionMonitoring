using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.BillingEntities;

namespace Presentation.Validators.BillingEntities;

public class BillingEntityRequestValidator : AbstractValidator<BillingEntityRequestDto>
{
    public BillingEntityRequestValidator(IBillingEntityService billingEntityService)
    {
        RuleFor(x => x.BillingEntityId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await billingEntityService.ExistsAsync(new BillingEntityId(x), new IncludeDisabled(true), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Billing entity not found");
    }
}
