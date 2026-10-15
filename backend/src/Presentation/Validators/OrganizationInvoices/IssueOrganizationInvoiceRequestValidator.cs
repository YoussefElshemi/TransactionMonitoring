using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.OrganizationInvoices;

namespace Presentation.Validators.OrganizationInvoices;

public class IssueOrganizationInvoiceRequestValidator : AbstractValidator<IssueOrganizationInvoiceRequestDto>
{
    public IssueOrganizationInvoiceRequestValidator(
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
    }
}
