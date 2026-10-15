using Core.ValueObjects;

namespace Core.Models.Cases;

public record EvidencePackPayment
{
    public required TransactionId TransactionId { get; init; }
    public required FriendlyName FriendlyName { get; init; }
    public required ExternalReference? ExternalReference { get; init; }
    public required CreatedAt CreatedAt { get; init; }
}
