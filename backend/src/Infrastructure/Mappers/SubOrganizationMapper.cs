using Core.Models.SubOrganizations;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class SubOrganizationMapper
{
    public static SubOrganization ToModel(this SubOrganizationEntity subOrganizationEntity)
    {
        return new SubOrganization
        {
            SubOrganizationId = new SubOrganizationId(subOrganizationEntity.SubOrganizationId),
            OrganizationId = new OrganizationId(subOrganizationEntity.OrganizationId),
            Name = new SubOrganizationName(subOrganizationEntity.Name),
            Description = !string.IsNullOrWhiteSpace(subOrganizationEntity.Description) ? new SubOrganizationDescription(subOrganizationEntity.Description) : null,
            ExternalReference = !string.IsNullOrWhiteSpace(subOrganizationEntity.ExternalReference) ? new ExternalReference(subOrganizationEntity.ExternalReference) : null,
            IsEnabled = new IsEnabled(subOrganizationEntity.IsEnabled),
            CreatedAt = new CreatedAt(subOrganizationEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(subOrganizationEntity.UpdatedAt),
            IsDeleted = new IsDeleted(subOrganizationEntity.IsDeleted),
            DeletedAt = subOrganizationEntity.DeletedAt.HasValue ? new DeletedAt(subOrganizationEntity.DeletedAt.Value) : null
        };
    }

    public static SubOrganizationEntity ToEntity(this SubOrganization subOrganization)
    {
        return new SubOrganizationEntity
        {
            SubOrganizationId = subOrganization.SubOrganizationId,
            OrganizationId = subOrganization.OrganizationId,
            Name = subOrganization.Name,
            Description = subOrganization.Description?.ToString(),
            ExternalReference = subOrganization.ExternalReference?.ToString(),
            IsEnabled = subOrganization.IsEnabled,
            CreatedAt = subOrganization.CreatedAt,
            UpdatedAt = subOrganization.UpdatedAt,
            IsDeleted = subOrganization.IsDeleted,
            DeletedAt = subOrganization.DeletedAt
        };
    }
}
