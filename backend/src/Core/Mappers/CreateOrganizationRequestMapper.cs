using Core.Models.Organizations;
using Core.ValueObjects;

namespace Core.Mappers;

public static class CreateOrganizationRequestMapper
{
    public static Organization ToModel(this CreateOrganizationRequest createOrganizationRequest, TimeProvider timeProvider)
    {
        return new Organization
        {
            SandboxMonthlyCredits = null,
            OrganizationId = new OrganizationId(Guid.NewGuid()),
            FriendlyName = createOrganizationRequest.FriendlyName,
            Status = createOrganizationRequest.Status,
            HoldWindow = createOrganizationRequest.HoldWindow,
            HoldExpiryVerdict = createOrganizationRequest.HoldExpiryVerdict,
            DeduplicationWindow = createOrganizationRequest.DeduplicationWindow,
            CaseGroupingPeriod = createOrganizationRequest.CaseGroupingPeriod,

            CaseGroupingEnabled = null,
            DeduplicationEnabled = null,
            CaseGroupingScope = null,
            CaseGroupingSeverityPolicy = null,
            MaxAlertsPerCase = null,

            DefaultLocale = null,
            DefaultTimeZone = null,
            CreatedAt = new CreatedAt(timeProvider.GetUtcNow()),
            UpdatedAt = new UpdatedAt(timeProvider.GetUtcNow()),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}