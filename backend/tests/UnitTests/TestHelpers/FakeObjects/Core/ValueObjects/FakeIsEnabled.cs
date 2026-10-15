using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeIsEnabled
{
    public static IsEnabled CreateValid(IFixture fixture)
    {
        return new IsEnabled(fixture.Create<bool>());
    }
}