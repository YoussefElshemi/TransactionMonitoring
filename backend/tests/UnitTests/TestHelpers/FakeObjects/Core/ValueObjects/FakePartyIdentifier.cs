using AutoFixture;
using Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

public static class FakePartyIdentifier
{
    public static PartyIdentifier CreateValid(IFixture fixture)
    {
        return new PartyIdentifier($"ACC-{fixture.Create<Guid>():N}");
    }
}
