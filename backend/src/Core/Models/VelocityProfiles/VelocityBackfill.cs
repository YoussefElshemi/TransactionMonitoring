using Core.Enums;
using Core.Models.BaseModels;
using Core.ValueObjects;

namespace Core.Models.VelocityProfiles;

public record VelocityBackfill : BaseModel
{
    public required VelocityBackfillId VelocityBackfillId { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required List<VelocityMetricName> Metrics { get; init; }
    public required VelocityBackfillStatus Status { get; init; }
    public required TotalCount TotalTransactions { get; init; }
    public required ProcessedCount ProcessedCount { get; init; }
    public required CompletedAt? CompletedAt { get; init; }

    public required Mode Mode { get; init; }
}
