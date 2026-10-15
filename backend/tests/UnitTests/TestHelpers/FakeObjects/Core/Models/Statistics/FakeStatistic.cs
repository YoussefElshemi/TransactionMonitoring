using AutoFixture;
using Core.Models.Statistics;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Statistics;

public static class FakeStatistic
{
    public static Statistic CreateValid(IFixture fixture)
    {
        return new Statistic
        {
            Case = FakeCaseStatistic.CreateValid(fixture),
            Usage = fixture.CreateMany<KeyValuePair<DateOnly, int>>().ToDictionary(x => x.Key, x => x.Value)
        };
    }
}