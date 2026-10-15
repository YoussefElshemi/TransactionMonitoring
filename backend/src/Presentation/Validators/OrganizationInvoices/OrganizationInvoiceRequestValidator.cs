using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.OrganizationInvoices;

namespace Presentation.Validators.OrganizationInvoices;

public class OrganizationInvoiceRequestValidator : AbstractValidator<OrganizationInvoiceRequestDto>
{
    public OrganizationInvoiceRequestValidator(
        IOrganizationService organizationService,
        IInvoiceService invoiceService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.InvoiceId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await invoiceService.ExistsAsync(new OrganizationId(x.OrganizationId), new InvoiceId(x.InvoiceId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Invoice not found");
    }
}