using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeAlertId
{
    public static AlertId CreateValid(IFixture fixture)
    {
        return new AlertId(fixture.Create<Guid>());
    }
}
