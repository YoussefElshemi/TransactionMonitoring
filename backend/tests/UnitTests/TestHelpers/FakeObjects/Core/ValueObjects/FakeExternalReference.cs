using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeExternalReference
{
    public static ExternalReference CreateValid(IFixture fixture)
    {
        return new ExternalReference(fixture.Create<string>());
    }
}
