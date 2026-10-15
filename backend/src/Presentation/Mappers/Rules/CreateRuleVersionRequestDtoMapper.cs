using Core.Enums;
using Core.Models.Rules;
using Core.ValueObjects;
using Presentation.Models.Rules;

namespace Presentation.Mappers.Rules;

public static class CreateRuleVersionRequestDtoMapper
{
    public static CreateRuleVersionRequest ToModel(this CreateRuleVersionRequestDto dto, ApplicationUserId createdByUserId)
    {
        return new CreateRuleVersionRequest
        {
            RuleId = new RuleId(dto.RuleId),
            RuleBody = dto.Body.RuleBody.ToModel(),
            Action = Enum.Parse<RuleAction>(dto.Body.Action, true),
            Severity = Enum.Parse<RuleSeverity>(dto.Body.Severity, true),
            RiskWeight = new RiskWeight(dto.Body.RiskWeight),
            CreatedByUserId = createdByUserId
        };
    }
}
