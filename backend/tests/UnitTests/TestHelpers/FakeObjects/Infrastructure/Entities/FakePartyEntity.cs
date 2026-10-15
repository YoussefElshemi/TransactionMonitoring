using AutoFixture;
using Core.Enums;
using Infrastructure.Entities;

namespace UnitTests.TestHelpers.FakeObjects.Infrastructure.Entities;

public static class FakePartyEntity
{
    public static PartyEntity CreateValid(IFixture fixture)
    {
        return new PartyEntity
        {
            ModeId = (int)Mode.Live,
            CreatedAt = fixture.Create<DateTimeOffset>(),
            UpdatedAt = fixture.Create<DateTimeOffset>(),
            PartyId = fixture.Create<Guid>(),
            OrganizationId = fixture.Create<Guid>(),
            Organization = FakeOrganizationEntity.CreateValid(fixture),
            Identifier = fixture.Create<string>(),
            DisplayName = fixture.Create<string>(),
            RiskScore = null,
            RiskNote = null,
            RiskSetByUserId = null,
            RiskSetAt = null,
            FirstSeenAt = fixture.Create<DateTimeOffset>(),
            LastSeenAt = fixture.Create<DateTimeOffset>(),
            Attributes = "{}"
        };
    }
}
