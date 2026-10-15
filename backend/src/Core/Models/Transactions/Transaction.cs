using System.Text.Json.Nodes;
using Core.Enums;
using Core.Models.BaseModels;
using Core.Models.Cases;
using Core.Models.Decisions;
using Core.Models.Parties;
using Core.Models.RuleEvaluations;
using Core.ValueObjects;

namespace Core.Models.Transactions;

public record Transaction : DeletableModel
{
    public required TransactionId TransactionId { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required FriendlyName FriendlyName { get; init; }
    public required ExternalReference? ExternalReference { get; init; }
    public required JsonObject Payload { get; init; }
    public required List<RuleEvaluation> RuleEvaluations { get; init; }
    public required List<Case> Cases { get; init; }
    public required List<TransactionParty> TransactionParties { get; init; }
    public required Decision? Decision { get; init; }

    public required Mode Mode { get; init; }
}
