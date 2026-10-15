using Core.Enums;
using Core.Models.Organizations;
using Core.ValueObjects;
using Infrastructure.Entities;

namespace Infrastructure.Mappers;

public static class OrganizationMapper
{
    public static Organization ToModel(this OrganizationEntity organizationEntity)
    {
        return new Organization
        {
            OrganizationId = new OrganizationId(organizationEntity.OrganizationId),
            FriendlyName = new FriendlyName(organizationEntity.FriendlyName),
            Status = (OrganizationStatus)organizationEntity.StatusId,
            HoldWindow = organizationEntity.HoldWindow.HasValue ? new HoldWindow(organizationEntity.HoldWindow.Value) : null,
            HoldExpiryVerdict = (HoldExpiryVerdict?)organizationEntity.HoldExpiryVerdictId,
            DeduplicationWindow = organizationEntity.DeduplicationWindow.HasValue ? new DeduplicationWindow(organizationEntity.DeduplicationWindow.Value) : null,
            CaseGroupingPeriod = organizationEntity.CaseGroupingPeriod.HasValue ? new CaseGroupingPeriod(organizationEntity.CaseGroupingPeriod.Value) : null,
            CaseGroupingEnabled = organizationEntity.CaseGroupingEnabled.HasValue ? new IsEnabled(organizationEntity.CaseGroupingEnabled.Value) : null,
            DeduplicationEnabled = organizationEntity.DeduplicationEnabled.HasValue ? new IsEnabled(organizationEntity.DeduplicationEnabled.Value) : null,
            CaseGroupingScope = organizationEntity.CaseGroupingScopeId.HasValue ? (CaseGroupingScope)organizationEntity.CaseGroupingScopeId.Value : null,
            CaseGroupingSeverityPolicy = organizationEntity.CaseGroupingSeverityPolicyId.HasValue ? (CaseGroupingSeverityPolicy)organizationEntity.CaseGroupingSeverityPolicyId.Value : null,
            MaxAlertsPerCase = organizationEntity.MaxAlertsPerCase.HasValue ? new MaxAlertsPerCase(organizationEntity.MaxAlertsPerCase.Value) : null,
            SandboxMonthlyCredits = organizationEntity.SandboxMonthlyCredits.HasValue ? new SandboxCredits(organizationEntity.SandboxMonthlyCredits.Value) : null,
            DefaultLocale = organizationEntity.DefaultLocale is not null ? new Locale(organizationEntity.DefaultLocale) : null,
            DefaultTimeZone = organizationEntity.DefaultTimeZone is not null ? new DisplayTimeZone(organizationEntity.DefaultTimeZone) : null,
            CreatedAt = new CreatedAt(organizationEntity.CreatedAt),
            UpdatedAt = new UpdatedAt(organizationEntity.UpdatedAt),
            IsDeleted = new IsDeleted(organizationEntity.IsDeleted),
            DeletedAt = organizationEntity.DeletedAt.HasValue ? new DeletedAt(organizationEntity.DeletedAt.Value) : null
        };
    }

    public static OrganizationEntity ToEntity(this Organization organization)
    {
        return new OrganizationEntity
        {
            OrganizationId = organization.OrganizationId,
            FriendlyName = organization.FriendlyName,
            StatusId = (int)organization.Status,
            HoldWindow = organization.HoldWindow,
            HoldExpiryVerdictId = (int?)organization.HoldExpiryVerdict,
            DeduplicationWindow = organization.DeduplicationWindow,
            CaseGroupingPeriod = organization.CaseGroupingPeriod,
            CaseGroupingEnabled = organization.CaseGroupingEnabled,
            DeduplicationEnabled = organization.DeduplicationEnabled,
            CaseGroupingScopeId = organization.CaseGroupingScope.HasValue ? (int)organization.CaseGroupingScope.Value : null,
            CaseGroupingSeverityPolicyId = organization.CaseGroupingSeverityPolicy.HasValue ? (int)organization.CaseGroupingSeverityPolicy.Value : null,
            MaxAlertsPerCase = organization.MaxAlertsPerCase,
            SandboxMonthlyCredits = organization.SandboxMonthlyCredits,
            DefaultLocale = organization.DefaultLocale?.ToString(),
            DefaultTimeZone = organization.DefaultTimeZone?.ToString(),
            CreatedAt = organization.CreatedAt,
            UpdatedAt = organization.UpdatedAt,
            IsDeleted = organization.IsDeleted,
            DeletedAt = organization.DeletedAt
        };
    }
}