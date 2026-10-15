using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeDeduplicationWindow
{
    public static DeduplicationWindow CreateValid(IFixture fixture)
    {
        return new DeduplicationWindow(fixture.Create<int>() % 1440 + 1);
    }
}
