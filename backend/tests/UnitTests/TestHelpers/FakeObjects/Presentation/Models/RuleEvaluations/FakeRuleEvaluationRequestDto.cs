using AutoFixture;
using Presentation.Models.RuleEvaluations;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.RuleEvaluations;

public static class FakeRuleEvaluationRequestDto
{
    public static RuleEvaluationRequestDto CreateValid(IFixture fixture)
    {
        return new RuleEvaluationRequestDto
        {
            RuleEvaluationId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>()
        };
    }
}
