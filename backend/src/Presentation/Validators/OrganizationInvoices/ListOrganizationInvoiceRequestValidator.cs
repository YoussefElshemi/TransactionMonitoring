using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Mappers.Invoices;
using Presentation.Models.OrganizationInvoices;
using Presentation.Validators.Common;

namespace Presentation.Validators.OrganizationInvoices;

public class ListOrganizationInvoiceRequestValidator(
    IOrganizationService organizationService,
    IInvoiceService invoiceService,
    TimeProvider timeProvider) : PagedRequestValidator<ListOrganizationInvoiceRequestDto>(timeProvider)
{
    protected sealed override void Initialize()
    {
        base.Initialize();

        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");
    }

    protected override Task<TotalCount> GetCountAsync(ListOrganizationInvoiceRequestDto request, CancellationToken stoppingToken)
    {
        return invoiceService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}