using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakePostalAddress
{
    public static PostalAddress CreateValid(IFixture fixture)
    {
        return new PostalAddress(fixture.Create<string>());
    }
}
