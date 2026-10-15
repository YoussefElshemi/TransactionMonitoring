using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakePartyId
{
    public static PartyId CreateValid(IFixture fixture)
    {
        return new PartyId(fixture.Create<Guid>());
    }
}
