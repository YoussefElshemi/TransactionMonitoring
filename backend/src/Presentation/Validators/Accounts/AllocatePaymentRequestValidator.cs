using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Accounts;

namespace Presentation.Validators.Accounts;

public class AllocatePaymentRequestValidator : AbstractValidator<AllocatePaymentRequestDto>
{
    public AllocatePaymentRequestValidator(IOrganizationService organizationService, IInvoiceService invoiceService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.Body.InvoiceId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, invoiceId, ct) => await invoiceService.ExistsAsync(
                new OrganizationId(x.OrganizationId), new InvoiceId(invoiceId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Invoice not found");

        RuleFor(x => x.Body.Amount)
            .GreaterThan(0)
            .When(x => x.Body.Amount.HasValue)
            .WithMessage("An allocation must be for more than nothing.");
    }
}
