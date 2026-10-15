using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeReportingEntityId
{
    public static ReportingEntityId CreateValid(IFixture fixture)
    {
        return new ReportingEntityId(fixture.Create<string>());
    }
}
