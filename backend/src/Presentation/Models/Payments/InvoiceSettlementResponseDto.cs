namespace Presentation.Models.Payments;

public record InvoiceSettlementResponseDto
{
    public required Guid InvoiceId { get; init; }
    public required decimal Amount { get; init; }
    public required decimal AllocatedAmount { get; init; }
    public required decimal CreditedAmount { get; init; }
    public required decimal OutstandingAmount { get; init; }

    public required bool IsSettled { get; init; }
}
