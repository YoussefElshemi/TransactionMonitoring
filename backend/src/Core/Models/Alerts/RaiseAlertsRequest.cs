using Core.ValueObjects;

namespace Core.Models.Alerts;

public record RaiseAlertsRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required TransactionId TransactionId { get; init; }
    public required RaisedAt RaisedAt { get; init; }
    public required List<AlertCandidate> Candidates { get; init; }
}
