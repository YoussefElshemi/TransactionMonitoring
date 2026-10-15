using AutoFixture;
using Core.Models.Cases;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.CaseRuleEvaluationLinks;

public static class FakeCaseRuleEvaluationLink
{
    public static CaseRuleEvaluationLink CreateValid(IFixture fixture)
    {
        return new CaseRuleEvaluationLink
        {
            CreatedAt = FakeCreatedAt.CreateValid(fixture),
            UpdatedAt = FakeUpdatedAt.CreateValid(fixture),
            CaseId = FakeCaseId.CreateValid(fixture),
            RuleEvaluationId = FakeRuleEvaluationId.CreateValid(fixture)
        };
    }
}