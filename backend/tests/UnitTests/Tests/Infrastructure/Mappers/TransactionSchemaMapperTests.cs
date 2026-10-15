using Core.Enums;
using Core.Models.TransactionSchemas;
using Core.ValueObjects;
using FluentAssertions;
using Infrastructure.Mappers;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.TransactionSchemas;

namespace UnitTests.Tests.Infrastructure.Mappers;

public class TransactionSchemaMapperTests : BaseTestClass
{
    [Fact]
    public void ToEntity_ThenToModel_RoundTripsTheSchema()
    {
        // Arrange
        var transactionSchema = FakeTransactionSchema.CreateValid(Fixture) with
        {
            Fields =
            [
                new SchemaField
                {
                    Name = new SchemaFieldName("amount"),
                    Type = SchemaFieldType.Number,
                    IsRequired = new IsRequired(true),
                    AllowedValues = null
                },
                new SchemaField
                {
                    Name = new SchemaFieldName("currency"),
                    Type = SchemaFieldType.String,
                    IsRequired = new IsRequired(false),
                    AllowedValues = ["GBP", "EUR"]
                }
            ]
        };

        // Act
        var actual = transactionSchema.ToEntity().ToModel();

        // Assert
        actual.TransactionSchemaId.Should().Be(transactionSchema.TransactionSchemaId);
        actual.OrganizationId.Should().Be(transactionSchema.OrganizationId);
        actual.CurrentVersionId.Should().Be(transactionSchema.CurrentVersionId);
        actual.CurrentVersionNumber.Should().Be(transactionSchema.CurrentVersionNumber);
        actual.CreatedByUserId.Should().Be(transactionSchema.CreatedByUserId);
        actual.CreatedAt.Should().Be(transactionSchema.CreatedAt);
        actual.UpdatedAt.Should().Be(transactionSchema.UpdatedAt);
        actual.Fields.Should().BeEquivalentTo(transactionSchema.Fields);
    }

    [Fact]
    public void ToEntity_StoresFieldsAsAnOpaqueString()
    {
        // Arrange
        var transactionSchema = FakeTransactionSchema.CreateValid(Fixture);

        // Act
        var entity = transactionSchema.ToEntity();

        // Assert
        entity.Fields.Should().NotBeNullOrWhiteSpace();
        entity.Fields.FieldsToModel().Should().BeEquivalentTo(transactionSchema.Fields);
    }

    [Fact]
    public void ToEntity_ThenToModel_RoundTripsAVersion()
    {
        // Arrange
        var transactionSchema = FakeTransactionSchema.CreateValid(Fixture);
        var version = new TransactionSchemaVersion
        {
            Mode = Mode.Live,
            TransactionSchemaVersionId = transactionSchema.CurrentVersionId,
            TransactionSchemaId = transactionSchema.TransactionSchemaId,
            OrganizationId = transactionSchema.OrganizationId,
            VersionNumber = transactionSchema.CurrentVersionNumber,
            Fields = transactionSchema.Fields,
            CreatedByUserId = transactionSchema.CreatedByUserId,
            CreatedAt = transactionSchema.CreatedAt
        };

        // Act
        var actual = version.ToEntity().ToModel();

        // Assert
        actual.Should().BeEquivalentTo(version);
    }
}
