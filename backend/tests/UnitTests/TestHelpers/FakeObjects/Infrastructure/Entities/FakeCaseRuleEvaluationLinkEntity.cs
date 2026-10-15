using AutoFixture;
using Infrastructure.Entities;

namespace UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

public static class FakeCaseRuleEvaluationLinkEntity
{
    public static CaseRuleEvaluationLinkEntity CreateValid(IFixture fixture)
    {
        return new CaseRuleEvaluationLinkEntity
        {
            CreatedAt = fixture.Create<DateTimeOffset>(),
            UpdatedAt = fixture.Create<DateTimeOffset>(),
            RuleEvaluationId = fixture.Create<Guid>(),
            CaseId = fixture.Create<Guid>()
        };
    }
}