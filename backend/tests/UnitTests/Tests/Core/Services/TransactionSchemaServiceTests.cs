using System.Text.Json.Nodes;
using Core.Enums;
using Core.Exceptions;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Models.Rules;
using Core.Models.TransactionSchemas;
using Core.Services;
using Core.ValueObjects;
using FluentAssertions;
using Moq;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.TransactionSchemas;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.Tests.Core.Services;

public class TransactionSchemaServiceTests : BaseTestClass
{
    private readonly Mock<ITransactionSchemaRepository> _mockTransactionSchemaRepository = new();
    private readonly Mock<ITransactionSchemaVersionRepository> _mockTransactionSchemaVersionRepository = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly TransactionSchemaService _sut;

    public TransactionSchemaServiceTests()
    {
        _sut = new TransactionSchemaService(
            _mockTransactionSchemaRepository.Object,
            _mockTransactionSchemaVersionRepository.Object,
            _mockUnitOfWork.Object,
            MockTimeProvider,
            TestModeContext);
    }

    [Fact]
    public async Task SetAsync_GivenNoExistingSchema_CreatesTheSchemaAtVersionOne()
    {
        // Arrange
        var organizationId = FakeOrganizationId.CreateValid(Fixture);
        var request = CreateRequest(organizationId);

        TransactionSchema? added = null;
        TransactionSchemaVersion? addedVersion = null;

        _mockTransactionSchemaRepository
            .SetupSequence(x => x.FindAsync(organizationId, It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransactionSchema?)null)
            .ReturnsAsync(FakeTransactionSchema.CreateValid(Fixture));

        _mockTransactionSchemaRepository
            .Setup(x => x.AddAsync(It.IsAny<TransactionSchema>(), It.IsAny<CancellationToken>()))
            .Callback<TransactionSchema, CancellationToken>((x, _) => added = x);

        _mockTransactionSchemaVersionRepository
            .Setup(x => x.AddAsync(It.IsAny<TransactionSchemaVersion>(), It.IsAny<CancellationToken>()))
            .Callback<TransactionSchemaVersion, CancellationToken>((x, _) => addedVersion = x);

        // Act
        await _sut.SetAsync(request, CancellationToken.None);

        // Assert
        added.Should().NotBeNull();
        added!.CurrentVersionNumber.Should().Be(new VersionNumber(1));
        addedVersion.Should().NotBeNull();
        addedVersion!.VersionNumber.Should().Be(new VersionNumber(1));
        addedVersion.TransactionSchemaId.Should().Be(added.TransactionSchemaId);
        addedVersion.TransactionSchemaVersionId.Should().Be(added.CurrentVersionId);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetAsync_GivenAnExistingSchema_AddsTheNextVersionAndRepointsTheSchema()
    {
        // Arrange
        var organizationId = FakeOrganizationId.CreateValid(Fixture);
        var existing = FakeTransactionSchema.CreateValid(Fixture) with { OrganizationId = organizationId };
        var request = CreateRequest(organizationId);

        TransactionSchemaVersion? addedVersion = null;
        VersionNumber? repointedTo = null;

        _mockTransactionSchemaRepository
            .Setup(x => x.FindAsync(organizationId, It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _mockTransactionSchemaVersionRepository
            .Setup(x => x.GetMaxVersionNumberAsync(existing.TransactionSchemaId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VersionNumber(4));

        _mockTransactionSchemaVersionRepository
            .Setup(x => x.AddAsync(It.IsAny<TransactionSchemaVersion>(), It.IsAny<CancellationToken>()))
            .Callback<TransactionSchemaVersion, CancellationToken>((x, _) => addedVersion = x);

        _mockTransactionSchemaRepository
            .Setup(x => x.ReplaceFieldsAsync(
                It.IsAny<OrganizationId>(), It.IsAny<List<SchemaField>>(), It.IsAny<TransactionSchemaVersionId>(), It.IsAny<VersionNumber>(), It.IsAny<CancellationToken>()))
            .Callback<OrganizationId, List<SchemaField>, TransactionSchemaVersionId, VersionNumber, CancellationToken>(
                (_, _, _, versionNumber, _) => repointedTo = versionNumber);

        // Act
        await _sut.SetAsync(request, CancellationToken.None);

        // Assert
        addedVersion.Should().NotBeNull();
        addedVersion!.VersionNumber.Should().Be(new VersionNumber(5));
        repointedTo.Should().Be(new VersionNumber(5));
        _mockTransactionSchemaRepository.Verify(x => x.AddAsync(It.IsAny<TransactionSchema>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetAsync_GivenAConcurrentSetLosingTheUniqueIndex_ThrowsAConflict()
    {
        // Arrange
        var organizationId = FakeOrganizationId.CreateValid(Fixture);
        var request = CreateRequest(organizationId);

        _mockTransactionSchemaRepository
            .Setup(x => x.FindAsync(organizationId, It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransactionSchema?)null);

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UniqueConstraintViolationException("IX_TransactionSchemas_OrganizationId", new Exception()));

        // Act
        var act = () => _sut.SetAsync(request, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<TransactionSchemaConflictException>();
        exception.Which.OrganizationId.Should().Be(organizationId);
    }

    [Fact]
    public async Task DeleteAsync_WithdrawsTheSchemaAndCommits()
    {
        // Arrange
        var organizationId = FakeOrganizationId.CreateValid(Fixture);

        // Act
        await _sut.DeleteAsync(organizationId, CancellationToken.None);

        // Assert
        _mockTransactionSchemaRepository.Verify(x => x.DeleteAsync(organizationId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RecoverAsync_RestoresTheSchemaAndCommits()
    {
        // Arrange
        var organizationId = FakeOrganizationId.CreateValid(Fixture);

        // Act
        await _sut.RecoverAsync(organizationId, CancellationToken.None);

        // Assert
        _mockTransactionSchemaRepository.Verify(x => x.RecoverAsync(organizationId, It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetAsync_LooksUpTheExistingSchemaIncludingDeletedOnes()
    {
        // Arrange
        var organizationId = FakeOrganizationId.CreateValid(Fixture);
        var request = CreateRequest(organizationId);

        _mockTransactionSchemaRepository
            .SetupSequence(x => x.FindAsync(organizationId, It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransactionSchema?)null)
            .ReturnsAsync(FakeTransactionSchema.CreateValid(Fixture));

        // Act
        await _sut.SetAsync(request, CancellationToken.None);

        // Assert
        _mockTransactionSchemaRepository.Verify(
            x => x.FindAsync(organizationId, It.Is<IncludeDeleted>(includeDeleted => includeDeleted), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task EnforcePayloadAsync_LooksUpTheLiveSchemaOnly()
    {
        // Arrange
        var organizationId = FakeOrganizationId.CreateValid(Fixture);

        _mockTransactionSchemaRepository
            .Setup(x => x.FindAsync(organizationId, It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransactionSchema?)null);

        // Act
        await _sut.EnforcePayloadAsync(organizationId, new JsonObject(), CancellationToken.None);

        // Assert
        _mockTransactionSchemaRepository.Verify(
            x => x.FindAsync(organizationId, It.Is<IncludeDeleted>(includeDeleted => !includeDeleted), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EnforceRuleBodyAsync_GivenNoSchema_DoesNotThrow()
    {
        // Arrange
        var organizationId = FakeOrganizationId.CreateValid(Fixture);

        _mockTransactionSchemaRepository
            .Setup(x => x.FindAsync(organizationId, It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransactionSchema?)null);

        var ruleBody = new RuleBodyModel
        {
            Field = "anythingAtAll",
            Condition = RuleCondition.EqualTo,
            Value = "x"
        };

        // Act
        var act = () => _sut.EnforceRuleBodyAsync(organizationId, ruleBody, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnforceRuleBodyAsync_GivenAnUnknownField_ThrowsCarryingTheViolation()
    {
        // Arrange
        var organizationId = FakeOrganizationId.CreateValid(Fixture);
        var schema = CreateSchemaWith(organizationId, "amount", SchemaFieldType.Number);

        _mockTransactionSchemaRepository
            .Setup(x => x.FindAsync(organizationId, It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(schema);

        var ruleBody = new RuleBodyModel
        {
            Field = "amont",
            Condition = RuleCondition.EqualTo,
            Value = 1
        };

        // Act
        var act = () => _sut.EnforceRuleBodyAsync(organizationId, ruleBody, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<RuleSchemaViolationException>();
        exception.Which.Violations.Should().ContainSingle().Which.Field.Should().Be("amont");
    }

    [Fact]
    public async Task EnforcePayloadAsync_GivenNoSchema_DoesNotThrow()
    {
        // Arrange
        var organizationId = FakeOrganizationId.CreateValid(Fixture);

        _mockTransactionSchemaRepository
            .Setup(x => x.FindAsync(organizationId, It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransactionSchema?)null);

        // Act
        var act = () => _sut.EnforcePayloadAsync(organizationId, new JsonObject(), CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task EnforcePayloadAsync_GivenAMissingRequiredField_ThrowsCarryingTheViolation()
    {
        // Arrange
        var organizationId = FakeOrganizationId.CreateValid(Fixture);
        var schema = CreateSchemaWith(organizationId, "amount", SchemaFieldType.Number);

        _mockTransactionSchemaRepository
            .Setup(x => x.FindAsync(organizationId, It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(schema);

        // Act
        var act = () => _sut.EnforcePayloadAsync(organizationId, new JsonObject(), CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<PayloadSchemaViolationException>();
        exception.Which.Violations.Should().ContainSingle().Which.Field.Should().Be("amount");
    }

    [Fact]
    public async Task EnforceAsync_GivenARuleBodyAndAPayload_ReadsTheSchemaOnce()
    {
        // Arrange
        var organizationId = FakeOrganizationId.CreateValid(Fixture);
        var schema = CreateSchemaWith(organizationId, "amount", SchemaFieldType.Number);

        _mockTransactionSchemaRepository
            .Setup(x => x.FindAsync(organizationId, It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(schema);

        var ruleBody = new RuleBodyModel
        {
            Field = "amount",
            Condition = RuleCondition.GreaterThan,
            Value = 1
        };

        var payload = new JsonObject
        {
            ["amount"] = 500
        };

        // Act
        await _sut.EnforceAsync(organizationId, ruleBody, payload, CancellationToken.None);

        // Assert
        _mockTransactionSchemaRepository.Verify(x => x.FindAsync(organizationId, It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnforceAsync_GivenAnUnknownField_ThrowsForTheRuleBodyBeforeThePayload()
    {
        // Arrange
        var organizationId = FakeOrganizationId.CreateValid(Fixture);
        var schema = CreateSchemaWith(organizationId, "amount", SchemaFieldType.Number);

        _mockTransactionSchemaRepository
            .Setup(x => x.FindAsync(organizationId, It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(schema);

        var ruleBody = new RuleBodyModel
        {
            Field = "amont",
            Condition = RuleCondition.EqualTo,
            Value = 1
        };

        // Act
        var act = () => _sut.EnforceAsync(organizationId, ruleBody, new JsonObject(), CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<RuleSchemaViolationException>();
        exception.Which.Violations.Should().ContainSingle().Which.Field.Should().Be("amont");
    }

    [Fact]
    public async Task EnforceAsync_GivenAValidRuleBodyAndAMissingRequiredField_ThrowsForThePayload()
    {
        // Arrange
        var organizationId = FakeOrganizationId.CreateValid(Fixture);
        var schema = CreateSchemaWith(organizationId, "amount", SchemaFieldType.Number);

        _mockTransactionSchemaRepository
            .Setup(x => x.FindAsync(organizationId, It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(schema);

        var ruleBody = new RuleBodyModel
        {
            Field = "amount",
            Condition = RuleCondition.GreaterThan,
            Value = 1
        };

        // Act
        var act = () => _sut.EnforceAsync(organizationId, ruleBody, new JsonObject(), CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<PayloadSchemaViolationException>();
        exception.Which.Violations.Should().ContainSingle().Which.Field.Should().Be("amount");
    }

    [Fact]
    public async Task EnforceAsync_GivenNoSchema_DoesNotThrow()
    {
        // Arrange
        var organizationId = FakeOrganizationId.CreateValid(Fixture);

        _mockTransactionSchemaRepository
            .Setup(x => x.FindAsync(organizationId, It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransactionSchema?)null);

        var ruleBody = new RuleBodyModel
        {
            Field = "anythingAtAll",
            Condition = RuleCondition.EqualTo,
            Value = "x"
        };

        // Act
        var act = () => _sut.EnforceAsync(organizationId, ruleBody, new JsonObject(), CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    private SetTransactionSchemaRequest CreateRequest(OrganizationId organizationId)
    {
        return new SetTransactionSchemaRequest
        {
            OrganizationId = organizationId,
            Fields = [FakeSchemaField.CreateValid(Fixture)],
            CreatedByUserId = FakeApplicationUserId.CreateValid(Fixture)
        };
    }

    private TransactionSchema CreateSchemaWith(OrganizationId organizationId, string fieldName, SchemaFieldType type)
    {
        return FakeTransactionSchema.CreateValid(Fixture) with
        {
            OrganizationId = organizationId,
            Fields =
            [
                new SchemaField
                {
                    Name = new SchemaFieldName(fieldName),
                    Type = type,
                    IsRequired = new IsRequired(true),
                    AllowedValues = null
                }
            ]
        };
    }
}
