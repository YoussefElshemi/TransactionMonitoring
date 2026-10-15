using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeDestination
{
    public static Destination CreateValid(IFixture fixture)
    {
        return new Destination(fixture.Create<Uri>().ToString());
    }
}
