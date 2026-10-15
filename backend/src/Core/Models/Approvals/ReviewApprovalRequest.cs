using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Approvals;

public record ReviewApprovalRequest
{
    public required ApprovalId ApprovalId { get; init; }

    public required ApprovalStatus Status { get; init; }
    public required Message? ReviewNote { get; init; }
    public required ApplicationUserId ReviewedByUserId { get; init; }
}
