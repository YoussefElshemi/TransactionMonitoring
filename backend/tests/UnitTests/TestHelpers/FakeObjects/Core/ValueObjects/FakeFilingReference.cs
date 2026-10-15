using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeFilingReference
{
    public static FilingReference CreateValid(IFixture fixture)
    {
        return new FilingReference(fixture.Create<string>());
    }
}
