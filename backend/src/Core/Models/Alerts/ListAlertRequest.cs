using Core.Enums;
using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.Alerts;

public record ListAlertRequest : PagedRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required FriendlyName? FriendlyName { get; init; }
    public required AlertStatus? Status { get; init; }
    public required RuleSeverity? Severity { get; init; }
    public required RuleId? RuleId { get; init; }
    public required PartyId? PartyId { get; init; }
    public required CaseId? CaseId { get; init; }
}
