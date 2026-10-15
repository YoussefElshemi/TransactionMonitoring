using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Invoices;

namespace Presentation.Validators.Invoices;

public class InvoiceRequestValidator : AbstractValidator<InvoiceRequestDto>
{
    public InvoiceRequestValidator(IInvoiceService invoiceService)
    {
        RuleFor(x => x.InvoiceId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await invoiceService.ExistsAsync(new OrganizationId(x.OrganizationId), new InvoiceId(x.InvoiceId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Invoice not found");
    }
}