using Core.Models.Organizations;
using Core.ValueObjects;
using Presentation.Models.Organizations;

namespace Presentation.Mappers.Organizations;

public static class UpdateOrganizationAlertPolicyRequestDtoMapper
{
    public static UpdateOrganizationRequest ToModel(this UpdateOrganizationAlertPolicyRequestDto updateOrganizationAlertPolicyRequestDto)
    {
        return new UpdateOrganizationRequest
        {
            OrganizationId = new OrganizationId(updateOrganizationAlertPolicyRequestDto.OrganizationId),
            FriendlyName = null,
            Status = null,
            HoldWindow = default,
            HoldExpiryVerdict = default,
            SandboxMonthlyCredits = default,
            DefaultLocale = default,
            DefaultTimeZone = default,
            CaseGroupingEnabled = updateOrganizationAlertPolicyRequestDto.Body.CaseGroupingEnabled.ToIsEnabled(),
            DeduplicationEnabled = updateOrganizationAlertPolicyRequestDto.Body.DeduplicationEnabled.ToIsEnabled(),
            CaseGroupingPeriod = updateOrganizationAlertPolicyRequestDto.Body.CaseGroupingPeriod.ToCaseGroupingPeriod(),
            DeduplicationWindow = updateOrganizationAlertPolicyRequestDto.Body.DeduplicationWindow.ToDeduplicationWindow(),
            CaseGroupingScope = updateOrganizationAlertPolicyRequestDto.Body.CaseGroupingScope.ToCaseGroupingScope(),
            CaseGroupingSeverityPolicy = updateOrganizationAlertPolicyRequestDto.Body.CaseGroupingSeverityPolicy.ToCaseGroupingSeverityPolicy(),
            MaxAlertsPerCase = updateOrganizationAlertPolicyRequestDto.Body.MaxAlertsPerCase.ToMaxAlertsPerCase()
        };
    }
}
