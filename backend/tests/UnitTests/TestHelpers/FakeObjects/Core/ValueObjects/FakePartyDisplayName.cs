using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakePartyDisplayName
{
    public static PartyDisplayName CreateValid(IFixture fixture)
    {
        return new PartyDisplayName(fixture.Create<string>());
    }
}
