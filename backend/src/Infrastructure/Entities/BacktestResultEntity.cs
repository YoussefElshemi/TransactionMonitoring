using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record BackTestResultEntity : BaseEntity
{
    public required Guid BackTestResultId { get; init; }
    public required Guid BackTestId { get; init; }
    public BackTestEntity BackTest { get; init; } = null!;
    public required Guid RuleEvaluationId { get; init; }
    public RuleEvaluationEntity RuleEvaluation { get; init; } = null!;
    public required bool OriginalMatched { get; init; }
    public required bool NewMatched { get; init; }
    public required bool HasChanged { get; init; }
}
