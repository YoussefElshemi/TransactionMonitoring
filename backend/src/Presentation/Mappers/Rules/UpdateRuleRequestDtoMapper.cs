using Core.Enums;
using Core.Models.Rules;
using Core.ValueObjects;
using Presentation.Models.Rules;

namespace Presentation.Mappers.Rules;

public static class UpdateRuleRequestDtoMapper
{
    public static UpdateRuleRequest ToModel(this UpdateRuleRequestDto updateRuleRequestDto)
    {
        return new UpdateRuleRequest
        {
            RuleId = new RuleId(updateRuleRequestDto.RuleId),
            FriendlyName = updateRuleRequestDto.Body.FriendlyName != null ? new FriendlyName(updateRuleRequestDto.Body.FriendlyName) : null,
            IsEnabled = updateRuleRequestDto.Body.IsEnabled.HasValue ? new IsEnabled(updateRuleRequestDto.Body.IsEnabled.Value) : null,
            Action = updateRuleRequestDto.Body.Action != null ? Enum.Parse<RuleAction>(updateRuleRequestDto.Body.Action, true) : null,
            Severity = updateRuleRequestDto.Body.Severity != null ? Enum.Parse<RuleSeverity>(updateRuleRequestDto.Body.Severity, true) : null,
            RiskWeight = updateRuleRequestDto.Body.RiskWeight.HasValue ? new RiskWeight(updateRuleRequestDto.Body.RiskWeight.Value) : null,
            HoldWindow = updateRuleRequestDto.Body.HoldWindow.ToHoldWindow(),
            HoldExpiryVerdict = updateRuleRequestDto.Body.HoldExpiryVerdict.ToHoldExpiryVerdict(),
            AlertSubjectRole = updateRuleRequestDto.Body.AlertSubjectRole.ToAlertSubjectRole(),
            DeduplicationWindow = updateRuleRequestDto.Body.DeduplicationWindow.ToDeduplicationWindow()
        };
    }
}
