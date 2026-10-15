using System.Text.Json.Nodes;
using Presentation.Models.Cases;
using Presentation.Models.Decisions;
using Presentation.Models.Parties;
using Presentation.Models.RuleEvaluations;

namespace Presentation.Models.Transactions;

public record TransactionResponseDto
{
    public required Guid TransactionId { get; init; }
    public required string FriendlyName { get; init; }
    public required string? ExternalReference { get; init; }
    public required Guid OrganizationId { get; init; }
    public required JsonObject Payload { get; init; }

    public required bool PayloadMasked { get; init; }
    public required RuleEvaluationResponseDto[] RuleEvaluations { get; init; }
    public required CaseResponseDto[] Cases { get; init; }
    public required TransactionPartyResponseDto[] Parties { get; init; }
    public required DecisionResponseDto? Decision { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }

    public required string Mode { get; init; }
}
