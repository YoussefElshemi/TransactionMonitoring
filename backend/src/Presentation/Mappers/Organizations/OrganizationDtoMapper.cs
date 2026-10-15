using Core.Models.Organizations;
using Presentation.Models.Organizations;

namespace Presentation.Mappers.Organizations;

public static class OrganizationDtoMapper
{
    public static OrganizationResponseDto ToDto(this Organization organization)
    {
        return new OrganizationResponseDto
        {
            OrganizationId = organization.OrganizationId,
            FriendlyName = organization.FriendlyName,
            Status = organization.Status.ToString(),
            HoldWindow = organization.HoldWindow,
            HoldExpiryVerdict = organization.HoldExpiryVerdict?.ToString(),
            CaseGroupingEnabled = organization.CaseGroupingEnabled.HasValue ? (bool)organization.CaseGroupingEnabled.Value : null,
            DeduplicationEnabled = organization.DeduplicationEnabled.HasValue ? (bool)organization.DeduplicationEnabled.Value : null,
            CaseGroupingPeriod = organization.CaseGroupingPeriod.HasValue ? (int)organization.CaseGroupingPeriod.Value : null,
            DeduplicationWindow = organization.DeduplicationWindow.HasValue ? (int)organization.DeduplicationWindow.Value : null,
            CaseGroupingScope = organization.CaseGroupingScope?.ToString(),
            CaseGroupingSeverityPolicy = organization.CaseGroupingSeverityPolicy?.ToString(),
            MaxAlertsPerCase = organization.MaxAlertsPerCase.HasValue ? (int)organization.MaxAlertsPerCase.Value : null,
            DefaultLocale = organization.DefaultLocale?.ToString(),
            DefaultTimeZone = organization.DefaultTimeZone?.ToString(),
            CreatedAt = organization.CreatedAt,
            UpdatedAt = organization.UpdatedAt
        };
    }
}