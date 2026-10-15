using AutoFixture;
using Core.Enums;
using Infrastructure.Entities;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

public static class FakeBackTestEntity
{
    public static BackTestEntity CreateValid(IFixture fixture)
    {
        return new BackTestEntity
        {
            ModeId = (int)Mode.Live,
            CreatedAt = fixture.Create<DateTimeOffset>(),
            UpdatedAt = fixture.Create<DateTimeOffset>(),
            BackTestId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>(),
            RuleId = fixture.Create<Guid>(),
            Rule = FakeRuleEntity.CreateValid(fixture),
            RuleVersionId = fixture.Create<Guid>(),
            Organization = FakeOrganizationEntity.CreateValid(fixture),
            StatusId = (int)BackTestStatus.Pending,
            SampleSize = FakeSampleSize.CreateValid(fixture),
            TotalEvaluations = fixture.Create<int>(),
            ProcessedCount = fixture.Create<int>(),
            CreatedByUserId = fixture.Create<Guid>(),
            CreatedByUser = FakeApplicationUserEntity.CreateValid(fixture),
            CompletedAt = fixture.Create<DateTimeOffset>()
        };
    }
}
