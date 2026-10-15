using Core.Enums;
using Core.Models.TransactionSchemas;
using Core.ValueObjects;
using FluentAssertions;
using Presentation.Mappers.TransactionSchemas;
using Presentation.Models.TransactionSchemas;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.TransactionSchemas;

namespace UnitTests.Tests.Presentation.Mappers.TransactionSchemas;

public class TransactionSchemaDtoMapperTests : BaseTestClass
{
    [Fact]
    public void ToDto_TransactionSchema()
    {
        // Arrange
        var transactionSchema = FakeTransactionSchema.CreateValid(Fixture);

        // Act
        var dto = transactionSchema.ToDto();

        // Assert
        dto.TransactionSchemaId.Should().Be(transactionSchema.TransactionSchemaId);
        dto.OrganizationId.Should().Be(transactionSchema.OrganizationId);
        dto.CurrentVersionNumber.Should().Be(transactionSchema.CurrentVersionNumber);
        dto.CreatedAt.Should().Be(transactionSchema.CreatedAt);
        dto.UpdatedAt.Should().Be(transactionSchema.UpdatedAt);
        dto.Fields.Should().HaveCount(transactionSchema.Fields.Count);
    }

    [Fact]
    public void ToDto_TransactionSchemaVersion()
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
        var dto = version.ToDto();

        // Assert
        dto.TransactionSchemaVersionId.Should().Be(version.TransactionSchemaVersionId);
        dto.TransactionSchemaId.Should().Be(version.TransactionSchemaId);
        dto.VersionNumber.Should().Be(version.VersionNumber);
        dto.CreatedAt.Should().Be(version.CreatedAt);
    }

    [Fact]
    public void ToModel_ThenToDto_RoundTripsTheFields()
    {
        // Arrange
        var fields = new List<SchemaFieldDto>
        {
            new() { Name = "amount", Type = nameof(SchemaFieldType.Number), IsRequired = true },
            new() { Name = "currency", Type = nameof(SchemaFieldType.String), IsRequired = false, AllowedValues = ["GBP", "EUR"] }
        };

        // Act
        var actual = fields.ToModel().ToDto();

        // Assert
        actual.Should().BeEquivalentTo(fields);
    }

    [Fact]
    public void ToModel_GivenALowercaseType_ParsesTheFieldType()
    {
        // Arrange
        var fields = new List<SchemaFieldDto>
        {
            new() { Name = "amount", Type = "number", IsRequired = true }
        };

        // Act
        var actual = fields.ToModel();

        // Assert
        actual.Should().ContainSingle()
            .Which.Type.Should().Be(SchemaFieldType.Number);
    }

    [Fact]
    public void ToModel_GivenAnEmptyAllowedValuesArray_LeavesTheFieldUnconstrained()
    {
        // Arrange
        var fields = new List<SchemaFieldDto>
        {
            new() { Name = "currency", Type = nameof(SchemaFieldType.String), IsRequired = false, AllowedValues = [] }
        };

        // Act
        var actual = fields.ToModel();

        // Assert
        actual.Should().ContainSingle()
            .Which.AllowedValues.Should().BeNull();
    }

    [Fact]
    public void ToDto_PreservesTheFieldName()
    {
        // Arrange
        var fields = new List<SchemaField>
        {
            new()
            {
                Name = new SchemaFieldName("beneficiaryCountry"),
                Type = SchemaFieldType.String,
                IsRequired = new IsRequired(true),
                AllowedValues = null
            }
        };

        // Act
        var actual = fields.ToDto();

        // Assert
        actual.Should().ContainSingle()
            .Which.Name.Should().Be("beneficiaryCountry");
    }
}
