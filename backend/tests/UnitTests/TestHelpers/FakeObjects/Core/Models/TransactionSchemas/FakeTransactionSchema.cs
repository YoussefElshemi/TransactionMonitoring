using AutoFixture;
using Core.Enums;
using Core.Models.TransactionSchemas;
using Core.ValueObjects;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.TestHelpers.FakeObjects.Core.Models.TransactionSchemas;

public static class FakeTransactionSchema
{
    public static TransactionSchema CreateValid(IFixture fixture)
    {
        return new TransactionSchema
        {
            Mode = Mode.Live,
            TransactionSchemaId = FakeTransactionSchemaId.CreateValid(fixture),
            OrganizationId = FakeOrganizationId.CreateValid(fixture),
            Fields = [FakeSchemaField.CreateValid(fixture)],
            CurrentVersionId = FakeTransactionSchemaVersionId.CreateValid(fixture),
            CurrentVersionNumber = FakeVersionNumber.CreateValid(fixture),
            CreatedByUserId = FakeApplicationUserId.CreateValid(fixture),
            CreatedAt = FakeCreatedAt.CreateValid(fixture),
            UpdatedAt = FakeUpdatedAt.CreateValid(fixture),
            IsDeleted = new IsDeleted(false),
            DeletedAt = null
        };
    }
}
