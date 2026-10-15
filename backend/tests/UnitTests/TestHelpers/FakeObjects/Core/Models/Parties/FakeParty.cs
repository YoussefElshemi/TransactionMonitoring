using AutoFixture;
using Core.Enums;
using Core.Models.Parties;
using Core.ValueObjects;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Parties;

public static class FakeParty
{
    public static Party CreateValid(IFixture fixture)
    {
        return new Party
        {
            Mode = Mode.Live,
            PartyId = FakePartyId.CreateValid(fixture),
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            Identifier = FakePartyIdentifier.CreateValid(fixture),
            DisplayName = FakePartyDisplayName.CreateValid(fixture),
            RiskScore = null,
            RiskNote = null,
            RiskSetByUserId = null,
            RiskSetAt = null,
            FirstSeenAt = new FirstSeenAt(fixture.Create<DateTimeOffset>()),
            LastSeenAt = new LastSeenAt(fixture.Create<DateTimeOffset>()),
            Attributes = [],
            Roles = [],
            CreatedAt = FakeCreatedAt.CreateValid(fixture),
            UpdatedAt = FakeUpdatedAt.CreateValid(fixture)
        };
    }
}
