using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.CaseReasonCodes;

public record CreateCaseReasonCodeRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required CaseReasonCodeName Code { get; init; }
    public required CaseReasonCodeDescription Description { get; init; }
    public required CaseDisposition? Disposition { get; init; }
    public required IsEnabled IsEnabled { get; init; }
}
