using AutoFixture;
using Core.Enums;
using Core.Models.RuleEvaluations;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Rules;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.RuleEvaluations;

public static class FakeRuleEvaluation
{
    public static RuleEvaluation CreateValid(IFixture fixture)
    {
        return new RuleEvaluation
        {
            Mode = Mode.Live,
            CreatedAt = FakeCreatedAt.CreateValid(fixture),
            UpdatedAt = FakeUpdatedAt.CreateValid(fixture),
            IsDeleted = FakeIsDeleted.CreateValid(fixture),
            DeletedAt = FakeDeletedAt.CreateValid(fixture),
            RuleEvaluationId = FakeRuleEvaluationId.CreateValid(fixture),
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            RuleId = FakeRuleId.CreateValid(fixture),
            Rule = FakeRule.CreateValid(fixture),
            RuleVersionId = FakeRuleVersionId.CreateValid(fixture),
            RuleVersion = FakeRuleVersion.CreateValid(fixture),
            TransactionId = FakeTransactionId.CreateValid(fixture),
            Matched = FakeMatched.CreateValid(fixture),
            ConditionMatches = [FakeConditionMatch.CreateValid(fixture)]
        };
    }
}
