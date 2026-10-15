using System.Text.Json.Nodes;
using Core.Enums;
using Core.Models.Parties;
using Core.ValueObjects;

namespace Core.Models.Queues;

public record RouteCaseRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required RuleSeverity Severity { get; init; }
    public required List<RuleId> RuleIds { get; init; }
    public required JsonObject Payload { get; init; }

    public required List<PartyFacts> Parties { get; init; }
}
