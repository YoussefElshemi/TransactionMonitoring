using System.Net;
using Core.Constants;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.BillingProfiles;

namespace Presentation.Validators.BillingProfiles;

public class SaveBillingProfileRequestValidator : AbstractValidator<SaveBillingProfileRequestDto>
{
    public SaveBillingProfileRequestValidator(
        IOrganizationService organizationService,
        IBillingEntityService billingEntityService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.Body.BillingEntityId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await billingEntityService.ExistsAsync(new BillingEntityId(x), new IncludeDisabled(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Billing entity not found");

        RuleFor(x => x.Body.LegalName).NotEmpty().MaximumLength(LegalName.MaxLength);
        RuleFor(x => x.Body.ContactName).NotEmpty().MaximumLength(ContactName.MaxLength);
        RuleFor(x => x.Body.ContactEmail).NotEmpty().EmailAddress();
        RuleFor(x => x.Body.Address).NotEmpty().MaximumLength(PostalAddress.MaxLength);

        RuleFor(x => x.Body.CountryCode)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(x => TaxJurisdictions.ByCountryCode.ContainsKey(x.Trim().ToUpperInvariant()))
            .WithMessage("CountryCode must be a jurisdiction this platform can determine tax for");

        RuleFor(x => x.Body.TaxIdentifier)
            .MaximumLength(TaxIdentifier.MaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Body.TaxIdentifier));

        RuleFor(x => x.Body.Currency).IsInEnum();

        RuleFor(x => x.Body.PaymentTerms)
            .InclusiveBetween(0, PaymentTerms.MaxDays)
            .WithMessage($"PaymentTerms must be between 0 and {PaymentTerms.MaxDays} days");

        RuleFor(x => x.Body.Locale)
            .Must(Locale.IsKnown!)
            .WithMessage("'Locale' must be a language tag this host knows, such as 'fr-FR'.")
            .When(x => !string.IsNullOrWhiteSpace(x.Body.Locale));
    }
}
