using Core.Enums;
using Core.Models.BaseModels;
using Core.Models.Cases;
using Core.ValueObjects;

namespace Core.Models.CaseLinks;

public record CaseLink : DeletableModel
{
    public required CaseLinkId CaseLinkId { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required CaseId CaseId { get; init; }
    public required CaseId LinkedCaseId { get; init; }

    public required Case? LinkedCase { get; init; }

    public required LinkReason? Reason { get; init; }
    public required ApplicationUserId CreatedByUserId { get; init; }

    public required Mode Mode { get; init; }
}
