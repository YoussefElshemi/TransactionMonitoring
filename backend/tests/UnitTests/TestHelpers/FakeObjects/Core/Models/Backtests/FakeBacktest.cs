using AutoFixture;
using Core.Enums;
using Core.Models.BackTests;
using Core.ValueObjects;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.BackTests;

public static class FakeBackTest
{
    public static BackTest CreateValid(IFixture fixture)
    {
        return new BackTest
        {
            Mode = Mode.Live,
            CreatedAt = FakeCreatedAt.CreateValid(fixture),
            UpdatedAt = FakeUpdatedAt.CreateValid(fixture),
            BackTestId = FakeBackTestId.CreateValid(fixture),
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            RuleId = FakeRuleId.CreateValid(fixture),
            RuleVersionId = FakeRuleVersionId.CreateValid(fixture),
            Status = BackTestStatus.Pending,
            SampleSize = FakeSampleSize.CreateValid(fixture),
            TotalEvaluations = new TotalCount(fixture.Create<int>()),
            ProcessedCount = new ProcessedCount(fixture.Create<int>()),
            CreatedByUserId = FakeApplicationUserId.CreateValid(fixture),
            CompletedAt = new CompletedAt(fixture.Create<DateTimeOffset>())
        };
    }
}
