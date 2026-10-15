using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeCaseGroupingPeriod
{
    public static CaseGroupingPeriod CreateValid(IFixture fixture)
    {
        return new CaseGroupingPeriod(fixture.Create<int>() % 1440 + 1);
    }
}
