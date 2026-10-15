using AutoFixture;
using Core.Enums;
using Core.Models.Statistics;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Statistics;

public static class FakeCaseStatistic
{
    public static CaseStatistic CreateValid(IFixture fixture)
    {
        var byStatus = Enum.GetValues<CaseStatus>()
            .ToDictionary(x => x, _ => new TotalCount(fixture.Create<int>()));

        return new CaseStatistic
        {
            Total = new TotalCount(byStatus.Values.Sum(x => x)),
            ByStatus = byStatus,
            AverageResolutionTime = fixture.Create<TimeSpan>(),
            Breached = new TotalCount(0),
            BreachRate = new BreachRate(0)
        };
    }
}
