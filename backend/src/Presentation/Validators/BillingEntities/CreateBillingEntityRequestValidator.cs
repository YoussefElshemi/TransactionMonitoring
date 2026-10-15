using Core.Constants;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.BillingEntities;

namespace Presentation.Validators.BillingEntities;

public class CreateBillingEntityRequestValidator : AbstractValidator<CreateBillingEntityRequestDto>
{
    public CreateBillingEntityRequestValidator()
    {
        RuleFor(x => x.Body.LegalName)
            .NotEmpty()
            .MaximumLength(LegalName.MaxLength);

        RuleFor(x => x.Body.Address)
            .NotEmpty()
            .MaximumLength(PostalAddress.MaxLength);

        RuleFor(x => x.Body.CountryCode)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(x => TaxJurisdictions.ByCountryCode.ContainsKey(x.Trim().ToUpperInvariant()))
            .WithMessage("CountryCode must be a jurisdiction this platform can determine tax for");

        RuleFor(x => x.Body.TaxIdentifier)
            .MaximumLength(TaxIdentifier.MaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Body.TaxIdentifier));

        RuleFor(x => x.Body.InvoiceNumberPrefix)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(NumberPrefix.MaxLength)
            .Matches("^[A-Z0-9]+$")
            .WithMessage("InvoiceNumberPrefix must be upper-case letters and digits");

        RuleFor(x => x.Body.CreditNoteNumberPrefix)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MaximumLength(NumberPrefix.MaxLength)
            .Matches("^[A-Z0-9]+$")
            .WithMessage("CreditNoteNumberPrefix must be upper-case letters and digits");

        RuleFor(x => x.Body.CreditNoteNumberPrefix)
            .NotEqual(x => x.Body.InvoiceNumberPrefix)
            .WithMessage("Credit notes need their own prefix, so a credit note number cannot be mistaken for an invoice number");

        RuleFor(x => x.Body.Currency).IsInEnum();
    }
}
