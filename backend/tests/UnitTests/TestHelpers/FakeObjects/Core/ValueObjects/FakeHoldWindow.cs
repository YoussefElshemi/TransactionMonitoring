using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeHoldWindow
{
    public static HoldWindow CreateValid(IFixture fixture)
    {
        return new HoldWindow(fixture.Create<int>() % 1440 + 1);
    }
}
