using Core.Enums;
using Core.Models.Rules;
using Core.ValueObjects;
using Presentation.Models.Rules;

namespace Presentation.Mappers.Rules;

public static class DeployRuleRequestDtoMapper
{
    public static DeployRuleRequest ToModel(this DeployRuleRequestDto dto, ApplicationUserId deployedByUserId)
    {
        return new DeployRuleRequest
        {
            RuleId = new RuleId(dto.RuleId),
            VersionNumber = new VersionNumber(dto.Body.VersionNumber),
            Strategy = Enum.Parse<DeploymentStrategy>(dto.Body.Strategy, true),
            StepPercentage = dto.Body.StepPercentage is { } stepPercentage ? new StepPercentage(stepPercentage) : null,
            StepIntervalSeconds = dto.Body.StepIntervalSeconds is { } stepIntervalSeconds ? new StepIntervalSeconds(stepIntervalSeconds) : null,
            DeployedByUserId = deployedByUserId
        };
    }
}
