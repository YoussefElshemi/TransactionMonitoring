using AutoFixture;
using Core.Enums;
using Core.Extensions;
using Core.Models.RuleEvaluations;
using Infrastructure.Entities;
using UnitTests.TestHelpers.FakeObjects.Core.Models.RuleEvaluations;

namespace UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

public static class FakeRuleEvaluationEntity
{
    public static RuleEvaluationEntity CreateValid(IFixture fixture)
    {
        return new RuleEvaluationEntity
        {
            ModeId = (int)Mode.Live,
            CreatedAt = fixture.Create<DateTimeOffset>(),
            UpdatedAt = fixture.Create<DateTimeOffset>(),
            IsDeleted = fixture.Create<bool>(),
            DeletedAt = fixture.Create<DateTimeOffset>(),
            RuleEvaluationId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>(),
            Organization = FakeOrganizationEntity.CreateValid(fixture),
            RuleId = fixture.Create<Guid>(),
            Rule = FakeRuleEntity.CreateValid(fixture),
            RuleVersionId = fixture.Create<Guid>(),
            RuleVersion = FakeRuleVersionEntity.CreateValid(fixture),
            TransactionId = fixture.Create<Guid>(),
            Transaction = FakeTransactionEntity.CreateValid(fixture),
            Matched = fixture.Create<bool>(),
            ConditionMatches = new List<ConditionMatch> { FakeConditionMatch.CreateValid(fixture) }.Serialize().ToBase64String()
        };
    }
}
