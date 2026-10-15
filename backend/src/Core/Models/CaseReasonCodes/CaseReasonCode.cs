using Core.Enums;
using Core.Models.BaseModels;
using Core.ValueObjects;

namespace Core.Models.CaseReasonCodes;

public record CaseReasonCode : DeletableModel
{
    public required CaseReasonCodeId CaseReasonCodeId { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required CaseReasonCodeName Code { get; init; }
    public required CaseReasonCodeDescription Description { get; init; }

    public required CaseDisposition? Disposition { get; init; }

    public required IsEnabled IsEnabled { get; init; }

    public required Mode Mode { get; init; }
}
