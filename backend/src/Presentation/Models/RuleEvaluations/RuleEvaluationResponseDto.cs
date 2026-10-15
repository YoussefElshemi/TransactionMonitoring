using Presentation.Models.Rules;

namespace Presentation.Models.RuleEvaluations;

public record RuleEvaluationResponseDto
{
    public required Guid RuleEvaluationId { get; init; }
    public required Guid OrganizationId { get; init; }
    public required Guid RuleId { get; init; }
    public required RuleVersionResponseDto RuleVersion { get; init; }
    public required Guid TransactionId { get; init; }
    public required bool Matched { get; init; }
    public required ConditionMatchResponseDto[]? ConditionMatches { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }

    public required string Mode { get; init; }
}
