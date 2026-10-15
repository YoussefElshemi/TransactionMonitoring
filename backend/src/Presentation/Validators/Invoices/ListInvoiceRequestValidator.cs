using Core.Interfaces.Services;
using Core.ValueObjects;
using Presentation.Mappers.Invoices;
using Presentation.Models.Invoices;
using Presentation.Validators.Common;

namespace Presentation.Validators.Invoices;

public class ListInvoiceRequestValidator(
    IInvoiceService invoiceService,
    TimeProvider timeProvider) : PagedRequestValidator<ListInvoiceRequestDto>(timeProvider)
{
    protected override Task<TotalCount> GetCountAsync(ListInvoiceRequestDto request, CancellationToken stoppingToken)
    {
        return invoiceService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}