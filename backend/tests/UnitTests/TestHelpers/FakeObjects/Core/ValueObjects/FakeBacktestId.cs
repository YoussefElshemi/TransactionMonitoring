using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeBackTestId
{
    public static BackTestId CreateValid(IFixture fixture)
    {
        return new BackTestId(fixture.Create<Guid>());
    }
}
