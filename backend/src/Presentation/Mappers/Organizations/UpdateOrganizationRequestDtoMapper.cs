using Core.Enums;
using Core.Models.Organizations;
using Core.ValueObjects;
using Presentation.Models.Organizations;

namespace Presentation.Mappers.Organizations;

public static class UpdateOrganizationRequestDtoMapper
{
    public static UpdateOrganizationRequest ToModel(this UpdateOrganizationRequestDto updateOrganizationRequestDto)
    {
        return new UpdateOrganizationRequest
        {
            OrganizationId = new OrganizationId(updateOrganizationRequestDto.OrganizationId),
            FriendlyName = updateOrganizationRequestDto.Body.FriendlyName != null ? new FriendlyName(updateOrganizationRequestDto.Body.FriendlyName) : null,
            Status = updateOrganizationRequestDto.Body.Status != null ? Enum.Parse<OrganizationStatus>(updateOrganizationRequestDto.Body.Status, true) : null,
            HoldWindow = updateOrganizationRequestDto.Body.HoldWindow.ToHoldWindow(),
            HoldExpiryVerdict = updateOrganizationRequestDto.Body.HoldExpiryVerdict.ToHoldExpiryVerdict(),
            SandboxMonthlyCredits = updateOrganizationRequestDto.Body.SandboxMonthlyCredits.ToSandboxCredits(),
            DefaultLocale = updateOrganizationRequestDto.Body.DefaultLocale.ToLocale(),
            DefaultTimeZone = updateOrganizationRequestDto.Body.DefaultTimeZone.ToDisplayTimeZone(),

            CaseGroupingEnabled = default,
            DeduplicationEnabled = default,
            CaseGroupingPeriod = default,
            DeduplicationWindow = default,
            CaseGroupingScope = default,
            CaseGroupingSeverityPolicy = default,
            MaxAlertsPerCase = default
        };
    }
}