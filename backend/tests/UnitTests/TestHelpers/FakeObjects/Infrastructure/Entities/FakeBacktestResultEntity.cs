using AutoFixture;
using Infrastructure.Entities;

namespace UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

public static class FakeBackTestResultEntity
{
    public static BackTestResultEntity CreateValid(IFixture fixture)
    {
        return new BackTestResultEntity
        {
            CreatedAt = fixture.Create<DateTimeOffset>(),
            UpdatedAt = fixture.Create<DateTimeOffset>(),
            BackTestResultId = fixture.Create<Guid>(),
            BackTestId = fixture.Create<Guid>(),
            RuleEvaluationId = fixture.Create<Guid>(),
            RuleEvaluation = FakeRuleEvaluationEntity.CreateValid(fixture),
            OriginalMatched = fixture.Create<bool>(),
            NewMatched = fixture.Create<bool>(),
            HasChanged = fixture.Create<bool>()
        };
    }
}
