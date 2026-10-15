using System.Text.Json.Nodes;
using Core.Exceptions;
using Core.Interfaces.Repositories;
using Core.Models.Transactions;
using Core.Services;
using Core.ValueObjects;
using FluentAssertions;
using Moq;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Transactions;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.Tests.Core.Services;

public class TransactionServiceTests : BaseTestClass
{
    private readonly Mock<ITransactionRepository> _mockTransactionRepository = new();
    private readonly TransactionService _sut;

    public TransactionServiceTests()
    {
        _sut = new TransactionService(
            _mockTransactionRepository.Object,
            MockTimeProvider,
            TestModeContext);

        _mockTransactionRepository
            .Setup(x => x.GenerateFriendlyName(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FriendlyName("TX0000000001"));
    }

    [Fact]
    public async Task GetOrCreateAsync_GivenAnUnseenExternalReference_CreatesTheTransaction()
    {
        // Arrange
        var createTransactionRequest = FakeCreateTransactionRequest.CreateValid(Fixture);

        _mockTransactionRepository
            .Setup(x => x.FindByExternalReferenceAsync(It.IsAny<OrganizationId>(), It.IsAny<ExternalReference>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        // Act
        var result = await _sut.GetOrCreateAsync(createTransactionRequest, CancellationToken.None);

        // Assert
        result.ExternalReference.Should().Be(createTransactionRequest.ExternalReference);
        result.FriendlyName.Should().Be(new FriendlyName("TX0000000001"));
        result.Payload.ToJsonString().Should().Be(createTransactionRequest.Payload.ToJsonString());
        _mockTransactionRepository.Verify(x => x.TryAddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetOrCreateAsync_GivenAKnownExternalReference_ReturnsTheExistingTransaction()
    {
        // Arrange
        var createTransactionRequest = FakeCreateTransactionRequest.CreateValid(Fixture);
        var existing = FakeTransaction.CreateValid(Fixture);

        _mockTransactionRepository
            .Setup(x => x.FindByExternalReferenceAsync(It.IsAny<OrganizationId>(), It.IsAny<ExternalReference>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        var result = await _sut.GetOrCreateAsync(createTransactionRequest, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(existing);
        _mockTransactionRepository.Verify(x => x.TryAddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetOrCreateAsync_GivenAKnownExternalReferenceAndADifferentPayload_Throws()
    {
        // Arrange
        var existing = FakeTransaction.CreateValid(Fixture);

        var createTransactionRequest = FakeCreateTransactionRequest.CreateValid(Fixture) with
        {
            ExternalReference = existing.ExternalReference,
            Payload = new JsonObject
            {
                ["Key"] = "A second look at the same payment"
            }
        };

        _mockTransactionRepository
            .Setup(x => x.FindByExternalReferenceAsync(It.IsAny<OrganizationId>(), It.IsAny<ExternalReference>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        var act = () => _sut.GetOrCreateAsync(createTransactionRequest, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<TransactionPayloadMismatchException>();
        exception.Which.ExternalReference.Should().Be(existing.ExternalReference!.Value);

        _mockTransactionRepository.Verify(x => x.TryAddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenTheRaceWinnerHoldsADifferentPayload_Throws()
    {
        // Arrange
        var winner = FakeTransaction.CreateValid(Fixture);
        var createTransactionRequest = FakeCreateTransactionRequest.CreateValid(Fixture) with
        {
            ExternalReference = winner.ExternalReference,
            Payload = new JsonObject
            {
                ["Key"] = "A second look at the same payment"
            }
        };

        _mockTransactionRepository
            .Setup(x => x.FindByExternalReferenceAsync(It.IsAny<OrganizationId>(), It.IsAny<ExternalReference>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        _mockTransactionRepository
            .Setup(x => x.TryAddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(winner);

        // Act
        var act = () => _sut.GetOrCreateAsync(createTransactionRequest, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<TransactionPayloadMismatchException>();
    }

    [Fact]
    public async Task GetOrCreateAsync_GivenAKnownExternalReferenceAndTheSamePayload_DoesNotThrow()
    {
        // Arrange
        var existing = FakeTransaction.CreateValid(Fixture);
        var createTransactionRequest = FakeCreateTransactionRequest.CreateValid(Fixture) with
        {
            Payload = existing.Payload.DeepClone().AsObject()
        };

        _mockTransactionRepository
            .Setup(x => x.FindByExternalReferenceAsync(It.IsAny<OrganizationId>(), It.IsAny<ExternalReference>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        // Act
        var result = await _sut.GetOrCreateAsync(createTransactionRequest, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(existing);
    }

    [Fact]
    public async Task GetOrCreateAsync_GivenNoExternalReference_CreatesWithoutLookingForOne()
    {
        // Arrange
        var createTransactionRequest = FakeCreateTransactionRequest.CreateValid(Fixture) with
        {
            ExternalReference = null
        };

        // Act
        var result = await _sut.GetOrCreateAsync(createTransactionRequest, CancellationToken.None);

        // Assert
        result.ExternalReference.Should().BeNull();
        result.FriendlyName.Should().Be(new FriendlyName("TX0000000001"));
        _mockTransactionRepository.Verify(
            x => x.FindByExternalReferenceAsync(It.IsAny<OrganizationId>(), It.IsAny<ExternalReference>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _mockTransactionRepository.Verify(x => x.TryAddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenAConcurrentCallerWinsTheRace_ReturnsTheirTransaction()
    {
        // Arrange
        var createTransactionRequest = FakeCreateTransactionRequest.CreateValid(Fixture);
        var winner = FakeTransaction.CreateValid(Fixture);

        _mockTransactionRepository
            .Setup(x => x.FindByExternalReferenceAsync(It.IsAny<OrganizationId>(), It.IsAny<ExternalReference>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction?)null);

        _mockTransactionRepository
            .Setup(x => x.TryAddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(winner);

        // Act
        var result = await _sut.GetOrCreateAsync(createTransactionRequest, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(winner);
    }

    [Fact]
    public async Task GetAsync_ReturnsTheTransaction()
    {
        // Arrange
        var transaction = FakeTransaction.CreateValid(Fixture);
        var transactionId = FakeTransactionId.CreateValid(Fixture);

        _mockTransactionRepository
            .Setup(x => x.GetAsync(It.IsAny<TransactionId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction);

        // Act
        var result = await _sut.GetAsync(transactionId, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(transaction);
    }

}
