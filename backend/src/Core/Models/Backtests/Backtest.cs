using Core.Enums;
using Core.Models.BaseModels;
using Core.ValueObjects;

namespace Core.Models.BackTests;

public record BackTest : BaseModel
{
    public required BackTestId BackTestId { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required RuleId RuleId { get; init; }
    public required RuleVersionId RuleVersionId { get; init; }
    public required BackTestStatus Status { get; init; }
    public required SampleSize SampleSize { get; init; }
    public required TotalCount TotalEvaluations { get; init; }
    public required ProcessedCount ProcessedCount { get; init; }
    public required ApplicationUserId CreatedByUserId { get; init; }
    public required CompletedAt? CompletedAt { get; init; }

    public required Mode Mode { get; init; }
}
