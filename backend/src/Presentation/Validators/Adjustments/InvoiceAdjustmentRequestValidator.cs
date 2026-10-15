using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Adjustments;

namespace Presentation.Validators.Adjustments;

public class InvoiceAdjustmentRequestValidator : AbstractValidator<InvoiceAdjustmentRequestDto>
{
    public InvoiceAdjustmentRequestValidator(
        IOrganizationService organizationService,
        IInvoiceAdjustmentService invoiceAdjustmentService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.InvoiceAdjustmentId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (dto, x, _, ct) =>
                await invoiceAdjustmentService.ExistsAsync(new OrganizationId(dto.OrganizationId), new InvoiceAdjustmentId(x), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Invoice adjustment not found");
    }
}
