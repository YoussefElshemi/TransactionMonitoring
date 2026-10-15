using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Plans;

public record PlanVersion
{
    public required PlanVersionId PlanVersionId { get; init; }
    public required PlanId PlanId { get; init; }
    public required VersionNumber VersionNumber { get; init; }

    public required Currency Currency { get; init; }

    public required CostPerRequest CostPerRequest { get; init; }

    public required PlatformFee PlatformFee { get; init; }

    public required MinimumCharge MinimumCharge { get; init; }

    public required List<PlanPrice> Prices { get; init; }
    public required ApplicationUserId? PublishedByUserId { get; init; }
    public required CreatedAt CreatedAt { get; init; }
}
