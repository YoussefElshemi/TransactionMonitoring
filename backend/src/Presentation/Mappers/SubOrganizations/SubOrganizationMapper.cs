using Core.Models.SubOrganizations;
using Presentation.Models.SubOrganizations;

namespace Presentation.Mappers.SubOrganizations;

public static class SubOrganizationMapper
{
    public static SubOrganizationResponseDto ToDto(this SubOrganization subOrganization)
    {
        return new SubOrganizationResponseDto
        {
            SubOrganizationId = subOrganization.SubOrganizationId,
            OrganizationId = subOrganization.OrganizationId,
            Name = subOrganization.Name,
            Description = subOrganization.Description?.ToString(),
            ExternalReference = subOrganization.ExternalReference?.ToString(),
            IsEnabled = subOrganization.IsEnabled,
            CreatedAt = subOrganization.CreatedAt,
            UpdatedAt = subOrganization.UpdatedAt
        };
    }

    public static SubOrganizationUsageResponseDto ToDto(this SubOrganizationUsage subOrganizationUsage)
    {
        return new SubOrganizationUsageResponseDto
        {
            SubOrganizationId = subOrganizationUsage.SubOrganizationId,
            Name = subOrganizationUsage.Name,
            UserCount = subOrganizationUsage.UserCount,
            QueueCount = subOrganizationUsage.QueueCount,
            RuleSetCount = subOrganizationUsage.RuleSetCount,
            RoleAssignmentCount = subOrganizationUsage.RoleAssignmentCount
        };
    }
}
