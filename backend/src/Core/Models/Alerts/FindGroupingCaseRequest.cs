using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Alerts;

public record FindGroupingCaseRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required PartyId PartyId { get; init; }
    public required RaisedAt RaisedAfter { get; init; }

    public required RuleId RuleId { get; init; }
    public required RuleSeverity Severity { get; init; }
    public required CaseGroupingScope Scope { get; init; }
    public required CaseGroupingSeverityPolicy SeverityPolicy { get; init; }
    public required MaxAlertsPerCase? MaxAlertsPerCase { get; init; }
}
