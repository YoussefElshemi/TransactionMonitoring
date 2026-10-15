using Core.ValueObjects;

namespace Core.Models.Payments;

public record AllocatePaymentRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required InvoiceId InvoiceId { get; init; }

    public required EntryAmount? Amount { get; init; }

    public required ApplicationUserId? AllocatedByUserId { get; init; }
}
