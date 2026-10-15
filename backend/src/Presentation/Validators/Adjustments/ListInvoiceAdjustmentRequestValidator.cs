using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Mappers.Adjustments;
using Presentation.Models.Adjustments;
using Presentation.Validators.Common;

namespace Presentation.Validators.Adjustments;

public class ListInvoiceAdjustmentRequestValidator(
    IOrganizationService organizationService,
    IInvoiceAdjustmentService invoiceAdjustmentService,
    TimeProvider timeProvider) : PagedRequestValidator<ListInvoiceAdjustmentRequestDto>(timeProvider)
{
    protected sealed override void Initialize()
    {
        base.Initialize();

        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");
    }

    protected override Task<TotalCount> GetCountAsync(ListInvoiceAdjustmentRequestDto request, CancellationToken stoppingToken)
    {
        return invoiceAdjustmentService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}
