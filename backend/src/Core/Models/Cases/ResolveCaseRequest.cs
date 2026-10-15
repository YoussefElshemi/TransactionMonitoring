using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Cases;

public record ResolveCaseRequest
{
    public required CaseId CaseId { get; init; }

    public required OrganizationId OrganizationId { get; init; }

    public required CaseStatus Status { get; init; }
    public required CaseDisposition Disposition { get; init; }
    public required CaseReasonCodeId? CaseReasonCodeId { get; init; }

    public required Message? Note { get; init; }
    public required ApplicationUserId ResolvedByUserId { get; init; }
}
