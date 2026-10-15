using AutoFixture;
using Presentation.Models.Statistics;

namespace UnitTests.TestHelpers.FakeObjects.Presentation.Models.Statistics;

public static class FakeStatisticRequestDto
{
    public static StatisticRequestDto CreateValid(IFixture fixture)
    {
        return new StatisticRequestDto
        {
            OrganizationId = fixture.Create<Guid>(),
            FromDate = fixture.Create<DateOnly>(),
            ToDate = fixture.Create<DateOnly>()
        };
    }
}