using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Payments;

public record RefundBalanceRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required Currency Currency { get; init; }
    public required EntryAmount Amount { get; init; }
    public required EntryDescription Reason { get; init; }
    public required ApplicationUserId? RefundedByUserId { get; init; }
}
