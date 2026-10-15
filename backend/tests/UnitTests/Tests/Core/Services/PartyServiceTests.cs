using System.Text.Json.Nodes;
using Core.Enums;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Models.Parties;
using Core.Models.PartyMappings;
using Core.Services;
using Core.ValueObjects;
using FluentAssertions;
using Moq;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Parties;
using UnitTests.TestHelpers.FakeObjects.Core.Models.PartyMappings;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.Tests.Core.Services;

public class PartyServiceTests : BaseTestClass
{
    private readonly Mock<IPartyRepository> _mockPartyRepository = new();
    private readonly Mock<ITransactionPartyRepository> _mockTransactionPartyRepository = new();
    private readonly Mock<IPartyMappingRepository> _mockPartyMappingRepository = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly PartyService _sut;

    public PartyServiceTests()
    {
        _sut = new PartyService(
            _mockPartyRepository.Object,
            _mockTransactionPartyRepository.Object,
            _mockPartyMappingRepository.Object,
            _mockUnitOfWork.Object,
            MockTimeProvider,
            TestModeContext);

        _mockTransactionPartyRepository
            .Setup(x => x.ListByTransactionAsync(It.IsAny<TransactionId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _mockPartyRepository
            .Setup(x => x.ListByIdentifiersAsync(It.IsAny<OrganizationId>(), It.IsAny<IReadOnlyCollection<PartyIdentifier>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _mockPartyRepository
            .Setup(x => x.TryAddAsync(It.IsAny<Party>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Party?)null);
    }

    [Fact]
    public async Task RecordAsync_GivenNoMapping_RecordsNothing()
    {
        // Arrange
        _mockPartyMappingRepository
            .Setup(x => x.FindAsync(It.IsAny<OrganizationId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PartyMapping?)null);

        // Act
        var links = await _sut.RecordAsync(CreateRequest(), CancellationToken.None);

        // Assert
        links.Should().BeEmpty();
        _mockPartyRepository.Verify(x => x.TryAddAsync(It.IsAny<Party>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RecordAsync_GivenAnUnseenParty_AddsThePartyAndTheLink()
    {
        // Arrange
        SetUpMapping();

        Party? added = null;
        List<TransactionParty>? linked = null;

        _mockPartyRepository
            .Setup(x => x.TryAddAsync(It.IsAny<Party>(), It.IsAny<CancellationToken>()))
            .Callback<Party, CancellationToken>((x, _) => added = x)
            .ReturnsAsync((Party?)null);

        _mockTransactionPartyRepository
            .Setup(x => x.AppendAsync(It.IsAny<List<TransactionParty>>(), It.IsAny<CancellationToken>()))
            .Callback<List<TransactionParty>, CancellationToken>((x, _) => linked = x);

        var request = CreateRequest();

        // Act
        await _sut.RecordAsync(request, CancellationToken.None);

        // Assert
        added.Should().NotBeNull();
        ((string)added!.Identifier).Should().Be("ACC-1");
        added.DisplayName!.Value.ToString().Should().Be("Ada");

        linked.Should().ContainSingle();
        linked![0].PartyId.Should().Be(added.PartyId);
        linked[0].Role.Should().Be(PartyRole.Remitter);
        linked[0].TransactionId.Should().Be(request.TransactionId);

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RecordAsync_GivenAPartyAlreadySeen_ReusesItRatherThanAddingASecond()
    {
        // Arrange
        SetUpMapping();

        var existing = FakeParty.CreateValid(Fixture) with
        {
            Identifier = new PartyIdentifier("ACC-1"),
            DisplayName = new PartyDisplayName("Ada")
        };

        _mockPartyRepository
            .Setup(x => x.ListByIdentifiersAsync(It.IsAny<OrganizationId>(), It.IsAny<IReadOnlyCollection<PartyIdentifier>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([existing]);

        List<TransactionParty>? linked = null;

        _mockTransactionPartyRepository
            .Setup(x => x.AppendAsync(It.IsAny<List<TransactionParty>>(), It.IsAny<CancellationToken>()))
            .Callback<List<TransactionParty>, CancellationToken>((x, _) => linked = x);

        // Act
        await _sut.RecordAsync(CreateRequest(), CancellationToken.None);

        // Assert
        _mockPartyRepository.Verify(x => x.TryAddAsync(It.IsAny<Party>(), It.IsAny<CancellationToken>()), Times.Never);
        linked.Should().ContainSingle();
        linked![0].PartyId.Should().Be(existing.PartyId);
    }

    [Fact]
    public async Task RecordAsync_GivenAPartyTheProviderHasRenamed_UpdatesTheDisplayName()
    {
        // Arrange
        SetUpMapping();

        var existing = FakeParty.CreateValid(Fixture) with
        {
            Identifier = new PartyIdentifier("ACC-1"),
            DisplayName = new PartyDisplayName("Ada Lovelace")
        };

        _mockPartyRepository
            .Setup(x => x.ListByIdentifiersAsync(It.IsAny<OrganizationId>(), It.IsAny<IReadOnlyCollection<PartyIdentifier>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([existing]);

        // Act
        await _sut.RecordAsync(CreateRequest(), CancellationToken.None);

        // Assert
        _mockPartyRepository.Verify(
            x => x.RecordSightingAsync(
                existing.PartyId,
                new PartyDisplayName("Ada"),
                It.IsAny<List<PartyAttributeValue>>(),
                It.IsAny<LastSeenAt>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RecordAsync_GivenAPartyAlreadySeen_MovesLastSeenAt()
    {
        // Arrange
        SetUpMapping();

        var existing = FakeParty.CreateValid(Fixture) with
        {
            Identifier = new PartyIdentifier("ACC-1"),
            DisplayName = new PartyDisplayName("Ada"),
            LastSeenAt = new LastSeenAt(MockTimeProvider.GetUtcNow().AddDays(-30))
        };

        _mockPartyRepository
            .Setup(x => x.ListByIdentifiersAsync(It.IsAny<OrganizationId>(), It.IsAny<IReadOnlyCollection<PartyIdentifier>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([existing]);

        // Act
        var facts = await _sut.RecordAsync(CreateRequest(), CancellationToken.None);

        // Assert
        _mockPartyRepository.Verify(
            x => x.RecordSightingAsync(
                existing.PartyId,
                It.IsAny<PartyDisplayName?>(),
                It.IsAny<List<PartyAttributeValue>>(),
                new LastSeenAt(MockTimeProvider.GetUtcNow()),
                It.IsAny<CancellationToken>()),
            Times.Once);

        facts.Should().ContainSingle();
        ((DateTimeOffset)facts[0].LastSeenAt).Should().Be(MockTimeProvider.GetUtcNow());
    }

    [Fact]
    public async Task RecordAsync_GivenTheLinkAlreadyExists_OffersItToTheAppendAnyway()
    {
        // Arrange
        SetUpMapping();

        var existing = FakeParty.CreateValid(Fixture) with { Identifier = new PartyIdentifier("ACC-1") };
        var request = CreateRequest();

        _mockPartyRepository
            .Setup(x => x.ListByIdentifiersAsync(It.IsAny<OrganizationId>(), It.IsAny<IReadOnlyCollection<PartyIdentifier>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([existing]);

        List<TransactionParty>? appended = null;

        _mockTransactionPartyRepository
            .Setup(x => x.AppendAsync(It.IsAny<List<TransactionParty>>(), It.IsAny<CancellationToken>()))
            .Callback<List<TransactionParty>, CancellationToken>((x, _) => appended = x);

        // Act
        var links = await _sut.RecordAsync(request, CancellationToken.None);

        // Assert
        appended.Should().ContainSingle();
        appended![0].PartyId.Should().Be(existing.PartyId);
        links.Should().ContainSingle();
    }

    [Fact]
    public async Task RecordAsync_GivenAConcurrentFirstSighting_LinksToTheWinner()
    {
        // Arrange
        SetUpMapping();

        var winner = FakeParty.CreateValid(Fixture) with { Identifier = new PartyIdentifier("ACC-1") };

        _mockPartyRepository
            .Setup(x => x.TryAddAsync(It.IsAny<Party>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(winner);

        List<TransactionParty>? linked = null;

        _mockTransactionPartyRepository
            .Setup(x => x.AppendAsync(It.IsAny<List<TransactionParty>>(), It.IsAny<CancellationToken>()))
            .Callback<List<TransactionParty>, CancellationToken>((x, _) => linked = x);

        // Act
        await _sut.RecordAsync(CreateRequest(), CancellationToken.None);

        // Assert
        linked.Should().ContainSingle();
        linked![0].PartyId.Should().Be(winner.PartyId);
    }

    [Fact]
    public async Task RecordAsync_GivenAPayloadThePathsDoNotFind_RecordsNothing()
    {
        // Arrange
        SetUpMapping();

        var request = CreateRequest(JsonNode.Parse("""{ "amount": 100 }""")!.AsObject());

        // Act
        var links = await _sut.RecordAsync(request, CancellationToken.None);

        // Assert
        links.Should().BeEmpty();
        _mockPartyRepository.Verify(x => x.TryAddAsync(It.IsAny<Party>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RecordAsync_GivenOneIdentifierInTwoRoles_WritesThePartyOnceAndLinksItTwice()
    {
        // Arrange
        _mockPartyMappingRepository
            .Setup(x => x.FindAsync(It.IsAny<OrganizationId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakePartyMapping.CreateValid(Fixture) with
            {
                Mappings =
                [
                    new PartyRoleMapping
                    {
                        Role = PartyRole.Remitter,
                        Path = new PartyPath("sender"),
                        IdentifierPath = new PartyPath("accountId"),
                        DisplayNamePath = null
                    },
                    new PartyRoleMapping
                    {
                        Role = PartyRole.Beneficiary,
                        Path = new PartyPath("beneficiary"),
                        IdentifierPath = new PartyPath("accountId"),
                        DisplayNamePath = null
                    }
                ]
            });

        List<TransactionParty>? linked = null;

        _mockTransactionPartyRepository
            .Setup(x => x.AppendAsync(It.IsAny<List<TransactionParty>>(), It.IsAny<CancellationToken>()))
            .Callback<List<TransactionParty>, CancellationToken>((x, _) => linked = x);

        var request = CreateRequest(JsonNode.Parse(
            """{ "sender": { "accountId": "ACC-SELF" }, "beneficiary": { "accountId": "ACC-SELF" } }""")!.AsObject());

        // Act
        await _sut.RecordAsync(request, CancellationToken.None);

        // Assert
        _mockPartyRepository.Verify(x => x.TryAddAsync(It.IsAny<Party>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockPartyRepository.Verify(
            x => x.RecordSightingAsync(
                It.IsAny<PartyId>(), It.IsAny<PartyDisplayName?>(), It.IsAny<List<PartyAttributeValue>>(),
                It.IsAny<LastSeenAt>(), It.IsAny<CancellationToken>()),
            Times.Never);

        linked.Should().HaveCount(2);
        linked!.Select(x => x.Role).Should().BeEquivalentTo([PartyRole.Remitter, PartyRole.Beneficiary]);
        linked!.Select(x => x.PartyId).Distinct().Should().ContainSingle();
    }

    private void SetUpMapping()
    {
        _mockPartyMappingRepository
            .Setup(x => x.FindAsync(It.IsAny<OrganizationId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakePartyMapping.CreateValid(Fixture) with
            {
                Mappings = [FakePartyMapping.CreateValidRoleMapping()]
            });
    }

    private RecordTransactionPartiesRequest CreateRequest(JsonObject? payload = null)
    {
        return new RecordTransactionPartiesRequest
        {
            OrganizationId = FakeOrganizationId.CreateValid(Fixture),
            TransactionId = FakeTransactionId.CreateValid(Fixture),
            Payload = payload ?? JsonNode.Parse("""{ "sender": { "accountId": "ACC-1", "name": "Ada" } }""")!.AsObject()
        };
    }
}
