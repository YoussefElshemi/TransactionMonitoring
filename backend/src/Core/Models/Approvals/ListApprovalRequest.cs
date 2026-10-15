using Core.Enums;
using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.Approvals;

public record ListApprovalRequest : PagedRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required ApprovalStatus? Status { get; init; }
    public required ApprovalSubject? Subject { get; init; }

    public required ApplicationUserId? RequestedByUserId { get; init; }
}
