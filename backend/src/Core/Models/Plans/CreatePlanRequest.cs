using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Plans;

public record CreatePlanRequest
{
    public required PlanName Name { get; init; }
    public required PlanDescription? Description { get; init; }
    public required OrganizationId? OrganizationId { get; init; }

    public required CostPerRequest CostPerRequest { get; init; }

    public required Currency Currency { get; init; }

    public required PlatformFee PlatformFee { get; init; }
    public required MinimumCharge MinimumCharge { get; init; }
    public required List<CreatePlanPriceRequest> Prices { get; init; }
    public required ApplicationUserId? CreatedByUserId { get; init; }
}
