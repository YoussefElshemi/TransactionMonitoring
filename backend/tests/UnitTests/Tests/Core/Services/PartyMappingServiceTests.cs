using Core.Enums;
using Core.Exceptions;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Models.PartyMappings;
using Core.Models.Rules;
using Core.Models.TransactionSchemas;
using Core.Services;
using Core.ValueObjects;
using FluentAssertions;
using Moq;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.PartyMappings;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Rules;
using UnitTests.TestHelpers.FakeObjects.Core.Models.TransactionSchemas;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.Tests.Core.Services;

public class PartyMappingServiceTests : BaseTestClass
{
    private readonly Mock<IPartyMappingRepository> _mockPartyMappingRepository = new();
    private readonly Mock<IPartyMappingVersionRepository> _mockPartyMappingVersionRepository = new();
    private readonly Mock<ITransactionSchemaRepository> _mockTransactionSchemaRepository = new();
    private readonly Mock<IRuleRepository> _mockRuleRepository = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly PartyMappingService _sut;

    public PartyMappingServiceTests()
    {
        _sut = new PartyMappingService(
            _mockPartyMappingRepository.Object,
            _mockPartyMappingVersionRepository.Object,
            _mockTransactionSchemaRepository.Object,
            _mockRuleRepository.Object,
            _mockUnitOfWork.Object,
            MockTimeProvider,
            TestModeContext);

        _mockTransactionSchemaRepository
            .Setup(x => x.FindAsync(It.IsAny<OrganizationId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransactionSchema?)null);

        _mockRuleRepository
            .Setup(x => x.ListByOrganizationAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    [Fact]
    public async Task SetAsync_GivenNoExistingMapping_CreatesItAtVersionOne()
    {
        // Arrange
        var organizationId = FakeOrganizationId.CreateValid(Fixture);
        var request = CreateRequest(organizationId);

        PartyMapping? added = null;
        PartyMappingVersion? addedVersion = null;

        _mockPartyMappingRepository
            .SetupSequence(x => x.FindAsync(organizationId, It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PartyMapping?)null)
            .ReturnsAsync(FakePartyMapping.CreateValid(Fixture));

        _mockPartyMappingRepository
            .Setup(x => x.AddAsync(It.IsAny<PartyMapping>(), It.IsAny<CancellationToken>()))
            .Callback<PartyMapping, CancellationToken>((x, _) => added = x);

        _mockPartyMappingVersionRepository
            .Setup(x => x.AddAsync(It.IsAny<PartyMappingVersion>(), It.IsAny<CancellationToken>()))
            .Callback<PartyMappingVersion, CancellationToken>((x, _) => addedVersion = x);

        // Act
        await _sut.SetAsync(request, CancellationToken.None);

        // Assert
        added.Should().NotBeNull();
        added!.CurrentVersionNumber.Should().Be(new VersionNumber(1));
        addedVersion.Should().NotBeNull();
        addedVersion!.VersionNumber.Should().Be(new VersionNumber(1));
        addedVersion.PartyMappingId.Should().Be(added.PartyMappingId);
        addedVersion.PartyMappingVersionId.Should().Be(added.CurrentVersionId);
    }

    [Fact]
    public async Task SetAsync_GivenAnExistingMapping_AddsTheNextVersionAndRepointsIt()
    {
        // Arrange
        var organizationId = FakeOrganizationId.CreateValid(Fixture);
        var existing = FakePartyMapping.CreateValid(Fixture) with { OrganizationId = organizationId };

        PartyMappingVersion? addedVersion = null;
        VersionNumber? repointedTo = null;

        _mockPartyMappingRepository
            .Setup(x => x.FindAsync(organizationId, It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _mockPartyMappingVersionRepository
            .Setup(x => x.GetMaxVersionNumberAsync(existing.PartyMappingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new VersionNumber(4));

        _mockPartyMappingVersionRepository
            .Setup(x => x.AddAsync(It.IsAny<PartyMappingVersion>(), It.IsAny<CancellationToken>()))
            .Callback<PartyMappingVersion, CancellationToken>((x, _) => addedVersion = x);

        _mockPartyMappingRepository
            .Setup(x => x.ReplaceMappingsAsync(
                organizationId,
                It.IsAny<List<PartyRoleMapping>>(),
                It.IsAny<PartyMappingVersionId>(),
                It.IsAny<VersionNumber>(),
                It.IsAny<CancellationToken>()))
            .Callback<OrganizationId, List<PartyRoleMapping>, PartyMappingVersionId, VersionNumber, CancellationToken>(
                (_, _, _, version, _) => repointedTo = version);

        // Act
        await _sut.SetAsync(CreateRequest(organizationId), CancellationToken.None);

        // Assert
        addedVersion!.VersionNumber.Should().Be(new VersionNumber(5));
        repointedTo.Should().Be(new VersionNumber(5));
        _mockPartyMappingRepository.Verify(x => x.AddAsync(It.IsAny<PartyMapping>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetAsync_GivenAConcurrentSet_ReportsAConflict()
    {
        // Arrange
        var organizationId = FakeOrganizationId.CreateValid(Fixture);

        _mockPartyMappingRepository
            .Setup(x => x.FindAsync(organizationId, It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PartyMapping?)null);

        _mockUnitOfWork
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UniqueConstraintViolationException("PartyMappings", new Exception()));

        // Act
        var act = () => _sut.SetAsync(CreateRequest(organizationId), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<PartyMappingConflictException>();
    }

    [Fact]
    public async Task SetAsync_GivenNoSchema_DoesNotEnforceOne()
    {
        // Arrange
        var organizationId = FakeOrganizationId.CreateValid(Fixture);

        _mockPartyMappingRepository
            .SetupSequence(x => x.FindAsync(organizationId, It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PartyMapping?)null)
            .ReturnsAsync(FakePartyMapping.CreateValid(Fixture));

        var request = CreateRequest(organizationId) with
        {
            Mappings =
            [
                new PartyRoleMapping
                {
                    Role = PartyRole.Remitter,
                    Path = null,
                    IdentifierPath = new PartyPath("nothingTheSchemaKnowsAbout"),
                    DisplayNamePath = null
                }
            ]
        };

        // Act
        var act = () => _sut.SetAsync(request, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SetAsync_GivenAPathTheSchemaDoesNotDeclare_IsRejected()
    {
        // Arrange
        var organizationId = FakeOrganizationId.CreateValid(Fixture);

        _mockTransactionSchemaRepository
            .Setup(x => x.FindAsync(organizationId, It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeTransactionSchema.CreateValid(Fixture) with
            {
                Fields =
                [
                    new SchemaField
                    {
                        Name = new SchemaFieldName("amount"),
                        Type = SchemaFieldType.Number,
                        IsRequired = new IsRequired(true),
                        AllowedValues = null
                    }
                ]
            });

        var request = CreateRequest(organizationId) with
        {
            Mappings =
            [
                new PartyRoleMapping
                {
                    Role = PartyRole.Remitter,
                    Path = null,
                    IdentifierPath = new PartyPath("senderAccountId"),
                    DisplayNamePath = null
                }
            ]
        };

        // Act
        var act = () => _sut.SetAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<PartyMappingSchemaViolationException>();
    }

    [Fact]
    public async Task SetAsync_GivenAnIdentifierPathThatIsNotAStringOrNumber_IsRejected()
    {
        // Arrange
        var organizationId = FakeOrganizationId.CreateValid(Fixture);

        _mockTransactionSchemaRepository
            .Setup(x => x.FindAsync(organizationId, It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeTransactionSchema.CreateValid(Fixture) with
            {
                Fields =
                [
                    new SchemaField
                    {
                        Name = new SchemaFieldName("isDomestic"),
                        Type = SchemaFieldType.Boolean,
                        IsRequired = new IsRequired(true),
                        AllowedValues = null
                    }
                ]
            });

        var request = CreateRequest(organizationId) with
        {
            Mappings =
            [
                new PartyRoleMapping
                {
                    Role = PartyRole.Remitter,
                    Path = null,
                    IdentifierPath = new PartyPath("isDomestic"),
                    DisplayNamePath = null
                }
            ]
        };

        // Act
        var act = () => _sut.SetAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<PartyMappingSchemaViolationException>();
    }

    [Fact]
    public async Task SetAsync_GivenPathsTheSchemaDeclares_IsAccepted()
    {
        // Arrange
        var organizationId = FakeOrganizationId.CreateValid(Fixture);

        _mockPartyMappingRepository
            .SetupSequence(x => x.FindAsync(organizationId, It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PartyMapping?)null)
            .ReturnsAsync(FakePartyMapping.CreateValid(Fixture));

        _mockTransactionSchemaRepository
            .Setup(x => x.FindAsync(organizationId, It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeTransactionSchema.CreateValid(Fixture) with
            {
                Fields =
                [
                    new SchemaField
                    {
                        Name = new SchemaFieldName("sender"),
                        Type = SchemaFieldType.Object,
                        IsRequired = new IsRequired(true),
                        AllowedValues = null,
                        Fields =
                        [
                            new SchemaField
                            {
                                Name = new SchemaFieldName("accountId"),
                                Type = SchemaFieldType.String,
                                IsRequired = new IsRequired(true),
                                AllowedValues = null
                            },
                            new SchemaField
                            {
                                Name = new SchemaFieldName("name"),
                                Type = SchemaFieldType.String,
                                IsRequired = new IsRequired(false),
                                AllowedValues = null
                            }
                        ]
                    }
                ]
            });

        // Act
        var act = () => _sut.SetAsync(CreateRequest(organizationId), CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteAsync_WhileARuleStillReadsAParty_IsRefused()
    {
        // Arrange
        var organizationId = FakeOrganizationId.CreateValid(Fixture);

        SetUpRules(PartyRuleBody(PartyRole.Remitter));

        // Act
        var act = () => _sut.DeleteAsync(organizationId, CancellationToken.None);

        // Assert
        var thrown = await act.Should().ThrowAsync<PartyMappingInUseException>();
        thrown.Which.Usages.Should().ContainSingle();
        thrown.Which.Usages[0].Roles.Should().Equal(PartyRole.Remitter);

        _mockPartyMappingRepository.Verify(
            x => x.DeleteAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WhenNoRuleReadsAParty_Withdraws()
    {
        // Arrange
        var organizationId = FakeOrganizationId.CreateValid(Fixture);

        SetUpRules(new RuleBody
        {
            Condition = RuleCondition.GreaterThan,
            Field = new RuleField("amount"),
            Value = new RuleValue(100),
            And = null,
            Or = null,
            Not = null
        });

        // Act
        await _sut.DeleteAsync(organizationId, CancellationToken.None);

        // Assert
        _mockPartyMappingRepository.Verify(
            x => x.DeleteAsync(organizationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetAsync_DroppingARoleARuleReads_IsRefused()
    {
        // Arrange
        var organizationId = FakeOrganizationId.CreateValid(Fixture);

        SetUpRules(PartyRuleBody(PartyRole.Beneficiary));

        var request = CreateRequest(organizationId) with
        {
            Mappings = [FakePartyMapping.CreateValidRoleMapping()]
        };

        // Act
        var act = () => _sut.SetAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<PartyMappingInUseException>();
    }

    [Fact]
    public async Task SetAsync_TurningASingleRoleIntoAManyRole_IsRefused()
    {
        // Arrange
        var organizationId = FakeOrganizationId.CreateValid(Fixture);

        SetUpRules(PartyRuleBody(PartyRole.Remitter));

        var request = CreateRequest(organizationId) with
        {
            Mappings =
            [
                new PartyRoleMapping
                {
                    Role = PartyRole.Remitter,
                    Path = new PartyPath("senders[*]"),
                    IdentifierPath = new PartyPath("accountId"),
                    DisplayNamePath = null
                }
            ]
        };

        // Act
        var act = () => _sut.SetAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<PartyMappingInUseException>();
    }

    [Fact]
    public async Task SetAsync_LeavingTheRoleARuleReadsIntact_IsAccepted()
    {
        // Arrange
        var organizationId = FakeOrganizationId.CreateValid(Fixture);

        SetUpRules(PartyRuleBody(PartyRole.Remitter));

        _mockPartyMappingRepository
            .SetupSequence(x => x.FindAsync(organizationId, It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PartyMapping?)null)
            .ReturnsAsync(FakePartyMapping.CreateValid(Fixture));

        var request = CreateRequest(organizationId) with
        {
            Mappings =
            [
                FakePartyMapping.CreateValidRoleMapping(),
                FakePartyMapping.CreateValidRoleMapping(PartyRole.Beneficiary)
            ]
        };

        // Act
        var act = () => _sut.SetAsync(request, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ListUsagesAsync_ReturnsOnlyTheRulesThatReadAParty()
    {
        // Arrange
        var organizationId = FakeOrganizationId.CreateValid(Fixture);

        SetUpRules(
            PartyRuleBody(PartyRole.Remitter),
            new RuleBody
            {
                Condition = RuleCondition.GreaterThan,
                Field = new RuleField("amount"),
                Value = new RuleValue(100),
                And = null,
                Or = null,
                Not = null
            });

        // Act
        var usages = await _sut.ListUsagesAsync(organizationId, CancellationToken.None);

        // Assert
        usages.Should().ContainSingle();
        usages[0].Roles.Should().Equal(PartyRole.Remitter);
    }

    private static RuleBody PartyRuleBody(PartyRole role)
    {
        return new RuleBody
        {
            Condition = RuleCondition.GreaterThan,
            Field = null,
            Party = new PartyCondition { Role = role, Attribute = PartyAttribute.RiskScore },
            Value = new RuleValue(70),
            And = null,
            Or = null,
            Not = null
        };
    }

    private void SetUpRules(params RuleBody[] ruleBodies)
    {
        _mockRuleRepository
            .Setup(x => x.ListByOrganizationAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ruleBodies.Select(x => FakeRule.CreateValid(Fixture) with { RuleBody = x }).ToList());
    }

    private SetPartyMappingRequest CreateRequest(OrganizationId organizationId)
    {
        return new SetPartyMappingRequest
        {
            OrganizationId = organizationId,
            Mappings = [FakePartyMapping.CreateValidRoleMapping()],
            CreatedByUserId = FakeApplicationUserId.CreateValid(Fixture)
        };
    }
}
