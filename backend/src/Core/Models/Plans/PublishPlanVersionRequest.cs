using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Plans;

public record PublishPlanVersionRequest
{
    public required PlanId PlanId { get; init; }
    public required CostPerRequest CostPerRequest { get; init; }
    public required Currency Currency { get; init; }
    public required PlatformFee PlatformFee { get; init; }
    public required MinimumCharge MinimumCharge { get; init; }
    public required List<CreatePlanPriceRequest> Prices { get; init; }
    public required ApplicationUserId? PublishedByUserId { get; init; }
}
