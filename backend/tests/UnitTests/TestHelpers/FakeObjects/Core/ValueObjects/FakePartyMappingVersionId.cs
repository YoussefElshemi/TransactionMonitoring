using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakePartyMappingVersionId
{
    public static PartyMappingVersionId CreateValid(IFixture fixture)
    {
        return new PartyMappingVersionId(fixture.Create<Guid>());
    }
}
