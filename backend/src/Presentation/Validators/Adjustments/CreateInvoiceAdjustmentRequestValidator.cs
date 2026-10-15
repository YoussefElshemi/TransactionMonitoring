using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Adjustments;

namespace Presentation.Validators.Adjustments;

public class CreateInvoiceAdjustmentRequestValidator : AbstractValidator<CreateInvoiceAdjustmentRequestDto>
{
    public CreateInvoiceAdjustmentRequestValidator(
        IOrganizationService organizationService,
        IInvoiceService invoiceService,
        TimeProvider timeProvider)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.Body.Month)
            .InclusiveBetween(1, 12);

        RuleFor(x => x.Body.Year)
            .InclusiveBetween(2000, timeProvider.GetUtcNow().Year + 1);

        RuleFor(x => x.Body.Description)
            .NotEmpty()
            .MaximumLength(InvoiceAdjustmentDescription.MaxLength);

        RuleFor(x => x.Body.Amount)
            .NotEqual(0)
            .WithMessage("An adjustment of nothing is not an adjustment");

        RuleFor(x => x.Body)
            .MustAsync(async (dto, _, __, ct) => !await invoiceService.ExistsByDateAsync(
                new OrganizationId(dto.OrganizationId),
                new Month(dto.Body.Month),
                new Year(dto.Body.Year),
                new IncludeDeleted(false),
                ct))
            .WithErrorCode(HttpStatusCode.Conflict.ToString())
            .WithMessage("That month has already been invoiced. Withdraw the invoice before raising an adjustment against it.");
    }
}
