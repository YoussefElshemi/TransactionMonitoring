using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeSuspiciousActivityReportId
{
    public static SuspiciousActivityReportId CreateValid(IFixture fixture)
    {
        return new SuspiciousActivityReportId(fixture.Create<Guid>());
    }
}
