using Presentation.Models.Invoices;

namespace Presentation.Models.OrganizationInvoices;

public record OrganizationInvoiceRequestDto : InvoiceRequestDto
{
    public new required Guid OrganizationId { get; init; }
}