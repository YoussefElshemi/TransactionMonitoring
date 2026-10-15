using AutoFixture;
using Core.Enums;
using Core.Models.PartyMappings;
using Core.ValueObjects;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.PartyMappings;

public static class FakePartyMapping
{
    public static PartyMapping CreateValid(IFixture fixture)
    {
        return new PartyMapping
        {
            Mode = Mode.Live,
            PartyMappingId = FakePartyMappingId.CreateValid(fixture),
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            Mappings = [CreateValidRoleMapping()],
            CurrentVersionId = FakePartyMappingVersionId.CreateValid(fixture),
            CurrentVersionNumber = new VersionNumber(1),
            CreatedByUserId = FakeApplicationUserId.CreateValid(fixture),
            CreatedAt = FakeCreatedAt.CreateValid(fixture),
            UpdatedAt = FakeUpdatedAt.CreateValid(fixture),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }

    public static PartyRoleMapping CreateValidRoleMapping(PartyRole role = PartyRole.Remitter)
    {
        return new PartyRoleMapping
        {
            Role = role,
            Path = new PartyPath("sender"),
            IdentifierPath = new PartyPath("accountId"),
            DisplayNamePath = new PartyPath("name")
        };
    }
}
