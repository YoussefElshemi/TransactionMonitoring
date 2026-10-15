using System.Text.Json.Nodes;
using Core.Enums;
using Core.Models.Decisions;
using Core.Models.Parties;
using Core.ValueObjects;

namespace Core.Models.Cases;

public record CreateCaseRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required RuleSeverity Severity { get; init; }
    public required TransactionId TransactionId { get; init; }
    public required PartyId? PartyId { get; init; }
    public required List<RuleEvaluationId> RuleEvaluationIds { get; init; }

    public required List<RuleId> RuleIds { get; init; }
    public required JsonObject Payload { get; init; }
    public required List<PartyFacts> Parties { get; init; }

    public required HoldPolicy? HoldPolicy { get; init; }
}
