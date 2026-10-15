using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.Invoices;

public record ListInvoiceRequest : PagedRequest
{
    public required OrganizationId OrganizationId { get; init; }
}