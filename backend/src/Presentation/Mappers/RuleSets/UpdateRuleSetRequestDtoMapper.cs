using Core.Models.RuleSets;
using Core.ValueObjects;
using Presentation.Models.RuleSets;

namespace Presentation.Mappers.RuleSets;

public static class UpdateRuleSetRequestDtoMapper
{
    public static UpdateRuleSetRequest ToModel(this UpdateRuleSetRequestDto updateRuleSetRequestDto)
    {
        return new UpdateRuleSetRequest
        {
            RuleSetId = new RuleSetId(updateRuleSetRequestDto.RuleSetId),
            FriendlyName = updateRuleSetRequestDto.Body.FriendlyName != null ? new FriendlyName(updateRuleSetRequestDto.Body.FriendlyName) : null,
            IsEnabled = updateRuleSetRequestDto.Body.IsEnabled.HasValue ? new IsEnabled(updateRuleSetRequestDto.Body.IsEnabled.Value) : null,
            ReviewThreshold = updateRuleSetRequestDto.Body.ReviewThreshold.HasValue ? new ReviewThreshold(updateRuleSetRequestDto.Body.ReviewThreshold.Value) : null,
            SubOrganizationId = updateRuleSetRequestDto.Body.SubOrganizationId.HasValue ? new SubOrganizationId(updateRuleSetRequestDto.Body.SubOrganizationId.Value) : null,
            ClearSubOrganization = new ClearCriteria(updateRuleSetRequestDto.Body.ClearSubOrganization ?? false)
        };
    }
}