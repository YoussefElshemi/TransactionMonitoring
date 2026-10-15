using System.Net;
using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Accounts;

namespace Presentation.Validators.Accounts;

public class RefundBalanceRequestValidator : AbstractValidator<RefundBalanceRequestDto>
{
    private const int MaxReasonLength = 500;

    public RefundBalanceRequestValidator(IOrganizationService organizationService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.Body.Currency)
            .Must(x => Enum.TryParse<Currency>(x, ignoreCase: true, out var currency) && Enum.IsDefined(currency))
            .WithMessage("Currency must be one of Gbp, Eur, Usd");

        RuleFor(x => x.Body.Amount)
            .GreaterThan(0)
            .WithMessage("A refund must be for more than nothing.");

        RuleFor(x => x.Body.Reason)
            .NotEmpty()
            .MaximumLength(MaxReasonLength);

        // The balance check belongs to the service; reading it here would race the write.
    }
}
