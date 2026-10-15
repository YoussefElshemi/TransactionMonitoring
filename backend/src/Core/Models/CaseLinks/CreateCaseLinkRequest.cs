using Core.ValueObjects;

namespace Core.Models.CaseLinks;

public record CreateCaseLinkRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required CaseId CaseId { get; init; }
    public required CaseId LinkedCaseId { get; init; }
    public required LinkReason? Reason { get; init; }
    public required ApplicationUserId CreatedByUserId { get; init; }
}
