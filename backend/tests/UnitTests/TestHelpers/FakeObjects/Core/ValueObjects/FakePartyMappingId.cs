using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakePartyMappingId
{
    public static PartyMappingId CreateValid(IFixture fixture)
    {
        return new PartyMappingId(fixture.Create<Guid>());
    }
}
