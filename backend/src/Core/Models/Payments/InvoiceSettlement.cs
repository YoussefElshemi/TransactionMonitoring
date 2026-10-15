using Core.ValueObjects;

namespace Core.Models.Payments;

public record InvoiceSettlement
{
    public required InvoiceId InvoiceId { get; init; }

    public required InvoiceAmount Amount { get; init; }

    public required InvoiceAmount AllocatedAmount { get; init; }
    public required InvoiceAmount CreditedAmount { get; init; }

    public required OutstandingAmount OutstandingAmount { get; init; }

    public required IsSettled IsSettled { get; init; }
}
