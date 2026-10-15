using Core.Models.OrganizationDataKeys;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class OrganizationDataKeyMapper
{
    public static OrganizationDataKey ToModel(this OrganizationDataKeyEntity organizationDataKeyEntity)
    {
        return new OrganizationDataKey
        {
            OrganizationId = new OrganizationId(organizationDataKeyEntity.OrganizationId),
            WrappedDataKey = new WrappedDataKey(organizationDataKeyEntity.WrappedDataKey),
            CreatedAt = new CreatedAt(organizationDataKeyEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(organizationDataKeyEntity.UpdatedAt)
        };
    }

    public static OrganizationDataKeyEntity ToEntity(this OrganizationDataKey organizationDataKey)
    {
        return new OrganizationDataKeyEntity
        {
            OrganizationId = organizationDataKey.OrganizationId,
            WrappedDataKey = organizationDataKey.WrappedDataKey,
            CreatedAt = organizationDataKey.CreatedAt,
            UpdatedAt = organizationDataKey.UpdatedAt
        };
    }
}
