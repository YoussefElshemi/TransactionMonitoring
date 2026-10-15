using Core.ValueObjects;

namespace Core.Models.Alerts;

public record FindDuplicateAlertRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required PartyId PartyId { get; init; }
    public required RuleId RuleId { get; init; }
    public required RaisedAt RaisedAfter { get; init; }
}
