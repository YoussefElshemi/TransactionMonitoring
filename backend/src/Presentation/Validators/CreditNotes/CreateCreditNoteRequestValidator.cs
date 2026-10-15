using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.CreditNotes;

namespace Presentation.Validators.CreditNotes;

public class CreateCreditNoteRequestValidator : AbstractValidator<CreateCreditNoteRequestDto>
{
    public CreateCreditNoteRequestValidator(
        IOrganizationService organizationService,
        IInvoiceService invoiceService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.InvoiceId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (dto, x, _, ct) =>
                await invoiceService.ExistsAsync(new OrganizationId(dto.OrganizationId), new InvoiceId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Invoice not found");

        RuleFor(x => x.Body.Reason)
            .NotEmpty()
            .MaximumLength(CreditNoteReason.MaxLength);

        RuleFor(x => x.Body.NetAmount)
            .GreaterThan(0)
            .WithMessage("A credit of nothing is not a credit")
            .When(x => x.Body.NetAmount.HasValue);
    }
}
