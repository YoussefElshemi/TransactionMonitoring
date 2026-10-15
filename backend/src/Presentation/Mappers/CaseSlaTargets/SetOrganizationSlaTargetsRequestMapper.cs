using Core.Enums;
using Core.Models.Cases;
using Core.ValueObjects;
using Presentation.Models.CaseSlaTargets;

namespace Presentation.Mappers.CaseSlaTargets;

public static class SetOrganizationSlaTargetsRequestMapper
{
    public static SetCaseSlaTargetsRequest ToModel(this SetOrganizationSlaTargetsRequestDto setOrganizationSlaTargetsRequestDto)
    {
        return new SetCaseSlaTargetsRequest
        {
            OrganizationId = new OrganizationId(setOrganizationSlaTargetsRequestDto.OrganizationId),
            Targets = setOrganizationSlaTargetsRequestDto.Body.Targets.ToDictionary(
                x => Enum.Parse<RuleSeverity>(x.Severity, true),
                x => new SlaTarget(x.TargetMinutes))
        };
    }
}
