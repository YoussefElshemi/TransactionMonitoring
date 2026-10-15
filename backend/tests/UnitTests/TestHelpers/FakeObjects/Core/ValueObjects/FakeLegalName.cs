using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakeLegalName
{
    public static LegalName CreateValid(IFixture fixture)
    {
        return new LegalName(fixture.Create<string>());
    }
}
