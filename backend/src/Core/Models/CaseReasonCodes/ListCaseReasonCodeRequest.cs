using Core.Enums;
using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.CaseReasonCodes;

public record ListCaseReasonCodeRequest : PagedRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required CaseReasonCodeName? Code { get; init; }
    public required CaseDisposition? Disposition { get; init; }
    public required IsEnabled? IsEnabled { get; init; }
}
