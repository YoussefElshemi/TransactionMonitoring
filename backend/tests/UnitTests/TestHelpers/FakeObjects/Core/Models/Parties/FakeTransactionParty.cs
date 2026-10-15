using AutoFixture;
using Core.Enums;
using Core.Models.Parties;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Parties;

public static class FakeTransactionParty
{
    public static TransactionParty CreateValid(IFixture fixture)
    {
        return new TransactionParty
        {
            TransactionId = FakeTransactionId.CreateValid(fixture),
            PartyId = FakePartyId.CreateValid(fixture),
            Role = PartyRole.Remitter,
            Party = null,
            CreatedAt = FakeCreatedAt.CreateValid(fixture),
            UpdatedAt = FakeUpdatedAt.CreateValid(fixture)
        };
    }
}
