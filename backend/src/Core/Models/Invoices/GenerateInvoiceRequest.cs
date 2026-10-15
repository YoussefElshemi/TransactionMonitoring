using Core.ValueObjects;

namespace Core.Models.Invoices;

public record GenerateInvoiceRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required Month Month { get; init; }
    public required Year Year { get; init; }
    public required ApplicationUserId? CreatedByUserId { get; init; }
}