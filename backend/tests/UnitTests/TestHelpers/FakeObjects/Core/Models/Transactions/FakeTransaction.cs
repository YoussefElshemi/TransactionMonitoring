using AutoFixture;
using Core.Enums;
using Core.Models.Transactions;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.Transactions;

public static class FakeTransaction
{
    public static Transaction CreateValid(IFixture fixture)
    {
        return new Transaction
        {
            Mode = Mode.Live,

            TransactionId = FakeTransactionId.CreateValid(fixture),
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            FriendlyName = FakeFriendlyName.CreateValid(fixture),
            ExternalReference = FakeExternalReference.CreateValid(fixture),
            Payload = FakeJsonObject.CreateValid(fixture),
            RuleEvaluations = [],
            Cases = [],
            TransactionParties = [],
            Decision = null,
            CreatedAt = FakeCreatedAt.CreateValid(fixture),
            UpdatedAt = FakeUpdatedAt.CreateValid(fixture),
            IsDeleted = FakeIsDeleted.CreateValid(fixture),
            DeletedAt = FakeDeletedAt.CreateValid(fixture),
        };
    }
}
