using Core.Enums;
using Core.Models.Usages;
using Core.ValueObjects;

namespace Core.Mappers;

public static class CreateUsageRequestMapper
{
    public static Usage ToModel(this CreateUsageRequest createUsageRequest, TimeProvider timeProvider, Mode mode)
    {
        return new Usage
        {
            Mode = mode,
            UsageId = new UsageId(Guid.NewGuid()),
            Resource = createUsageRequest.Resource,
            BillableResource = createUsageRequest.BillableResource,
            OrganizationId = createUsageRequest.OrganizationId,
            Count = createUsageRequest.Count,
            CreatedAt = new CreatedAt(timeProvider.GetUtcNow()),
            UpdatedAt = new UpdatedAt(timeProvider.GetUtcNow())
        };
    }
}