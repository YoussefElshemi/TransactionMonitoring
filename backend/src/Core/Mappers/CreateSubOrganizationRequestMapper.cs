using Core.Models.SubOrganizations;
using Core.ValueObjects;

namespace Core.Mappers;

public static class CreateSubOrganizationRequestMapper
{
    public static SubOrganization ToModel(this CreateSubOrganizationRequest createSubOrganizationRequest, TimeProvider timeProvider)
    {
        return new SubOrganization
        {
            SubOrganizationId = new SubOrganizationId(Guid.NewGuid()),
            OrganizationId = createSubOrganizationRequest.OrganizationId,
            Name = createSubOrganizationRequest.Name,
            Description = createSubOrganizationRequest.Description,
            ExternalReference = createSubOrganizationRequest.ExternalReference,
            IsEnabled = createSubOrganizationRequest.IsEnabled,
            CreatedAt = new CreatedAt(timeProvider.GetUtcNow()),
            UpdatedAt = new UpdatedAt(timeProvider.GetUtcNow()),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}
