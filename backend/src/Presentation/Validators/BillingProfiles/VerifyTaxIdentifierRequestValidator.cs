using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.BillingProfiles;

namespace Presentation.Validators.BillingProfiles;

public class VerifyTaxIdentifierRequestValidator : AbstractValidator<VerifyTaxIdentifierRequestDto>
{
    public VerifyTaxIdentifierRequestValidator(
        IOrganizationService organizationService,
        IBillingProfileService billingProfileService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found")
            .MustAsync(async (x, ct) => await billingProfileService.ExistsAsync(new OrganizationId(x), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Billing profile not found");
    }
}
