using Core.Models.Organizations;
using Core.ValueObjects;
using Presentation.Models.Organizations;

namespace Presentation.Mappers.Organizations;

public static class UpdateOrganizationHoldPolicyRequestDtoMapper
{
    public static UpdateOrganizationRequest ToModel(this UpdateOrganizationHoldPolicyRequestDto updateOrganizationHoldPolicyRequestDto)
    {
        return new UpdateOrganizationRequest
        {
            OrganizationId = new OrganizationId(updateOrganizationHoldPolicyRequestDto.OrganizationId),
            FriendlyName = null,
            Status = null,
            HoldWindow = updateOrganizationHoldPolicyRequestDto.Body.HoldWindow.ToHoldWindow(),
            HoldExpiryVerdict = updateOrganizationHoldPolicyRequestDto.Body.HoldExpiryVerdict.ToHoldExpiryVerdict(),
            CaseGroupingEnabled = default,
            DeduplicationEnabled = default,
            CaseGroupingPeriod = default,
            DeduplicationWindow = default,
            CaseGroupingScope = default,
            CaseGroupingSeverityPolicy = default,
            MaxAlertsPerCase = default,
            SandboxMonthlyCredits = default,
            DefaultLocale = default,
            DefaultTimeZone = default
        };
    }
}
