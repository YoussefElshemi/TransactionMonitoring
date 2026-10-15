using AutoFixture;
using Core.Models.Statistics;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Statistics;

public static class FakeStatisticsRequest
{
    public static StatisticRequest CreateValid(IFixture fixture)
    {
        return new StatisticRequest
        {
            FromDate = FakeDateRange.CreateValid(fixture),
            ToDate = FakeDateRange.CreateValid(fixture),
            OrganizationId = FakeOrganizationId.CreateValid(fixture)
        };
    }
}