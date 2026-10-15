using System.Net;
using Core.Constants;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.BillingEntities;

namespace Presentation.Validators.BillingEntities;

public class UpdateBillingEntityRequestValidator : AbstractValidator<UpdateBillingEntityRequestDto>
{
    public UpdateBillingEntityRequestValidator(IBillingEntityService billingEntityService)
    {
        RuleFor(x => x.BillingEntityId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await billingEntityService.ExistsAsync(new BillingEntityId(x), new IncludeDisabled(true), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Billing entity not found");

        RuleFor(x => x.Body.LegalName)
            .MaximumLength(LegalName.MaxLength)
            .When(x => x.Body.LegalName is not null);

        RuleFor(x => x.Body.Address)
            .MaximumLength(PostalAddress.MaxLength)
            .When(x => x.Body.Address is not null);

        RuleFor(x => x.Body.CountryCode)
            .Must(x => TaxJurisdictions.ByCountryCode.ContainsKey(x!.Trim().ToUpperInvariant()))
            .WithMessage("CountryCode must be a jurisdiction this platform can determine tax for")
            .When(x => !string.IsNullOrWhiteSpace(x.Body.CountryCode));

        RuleFor(x => x.Body.TaxIdentifier)
            .MaximumLength(TaxIdentifier.MaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Body.TaxIdentifier));

        RuleFor(x => x.Body.Currency).IsInEnum().When(x => x.Body.Currency.HasValue);
    }
}
