using Core.Enums;
using Core.Models.Usages;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class UsageMapper
{
    public static Usage ToModel(this UsageEntity usageEntity)
    {
        return new Usage
        {
            Mode = (Mode)usageEntity.ModeId,
            UsageId = new UsageId(usageEntity.UsageId),
            Resource = new Resource(usageEntity.Resource),
            BillableResource = (BillableResource)usageEntity.BillableResourceId,
            OrganizationId = new OrganizationId(usageEntity.OrganizationId),
            Count = new Count(usageEntity.Count),
            CreatedAt = new CreatedAt(usageEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(usageEntity.UpdatedAt)
        };
    }

    public static UsageEntity ToEntity(this Usage usage)
    {
        return new UsageEntity
        {
            ModeId = (int)usage.Mode,
            UsageId = usage.UsageId,
            Resource = usage.Resource,
            BillableResourceId = (int)usage.BillableResource,
            OrganizationId = usage.OrganizationId,
            Count = usage.Count,
            CreatedAt = usage.CreatedAt,
            UpdatedAt = usage.UpdatedAt
        };
    }
}