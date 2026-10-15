using AutoFixture;
using Core.Enums;
using Presentation.Models.Rules;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Rules;

public static class FakeDeployRuleRequestDto
{
    public static DeployRuleRequestDto CreateValid(IFixture fixture)
    {
        return new DeployRuleRequestDto
        {
            RuleId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>(),
            Body = new DeployRuleRequestBodyDto
            {
                VersionNumber = 2,
                Strategy = nameof(DeploymentStrategy.AllAtOnce),
                StepPercentage = null,
                StepIntervalSeconds = null
            }
        };
    }

    public static DeployRuleRequestDto CreateValidGradual(IFixture fixture)
    {
        return new DeployRuleRequestDto
        {
            RuleId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>(),
            Body = new DeployRuleRequestBodyDto
            {
                VersionNumber = 2,
                Strategy = nameof(DeploymentStrategy.Gradual),
                StepPercentage = 20,
                StepIntervalSeconds = 60
            }
        };
    }
}
