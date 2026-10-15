using Core.Exceptions;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Models.Commitments;
using Core.Services;
using Core.ValueObjects;
using FluentAssertions;
using Moq;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Commitments;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.Tests.Core.Services;

public class CommitmentServiceTests : BaseTestClass
{
    private readonly Mock<ICommitmentRepository> _mockCommitmentRepository = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly CommitmentService _sut;

    public CommitmentServiceTests()
    {
        _mockCommitmentRepository
            .Setup(x => x.OverlapsAsync(It.IsAny<OrganizationId>(), It.IsAny<EffectiveFrom>(), It.IsAny<EffectiveTo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        _mockCommitmentRepository
            .Setup(x => x.GetAsync(It.IsAny<CommitmentId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeCommitment.CreateValid(Fixture));

        _sut = new CommitmentService(_mockCommitmentRepository.Object, _mockUnitOfWork.Object, MockTimeProvider);
    }

    [Fact]
    public async Task CreateAsync_GivenAValidTerm_WritesItAndSaves()
    {
        // Arrange
        var request = FakeCreateCommitmentRequest.CreateValid(Fixture);

        // Act
        await _sut.CreateAsync(request, CancellationToken.None);

        // Assert
        _mockCommitmentRepository.Verify(x => x.AddAsync(It.IsAny<Commitment>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_GivenATermThatEndsBeforeItStarts_Throws()
    {
        // Arrange
        var effectiveFrom = FakeEffectiveFrom.CreateValid(Fixture);
        var request = FakeCreateCommitmentRequest.CreateValid(Fixture) with
        {
            EffectiveFrom = effectiveFrom,
            EffectiveTo = new EffectiveTo(((DateTimeOffset)effectiveFrom).AddDays(-1))
        };

        // Act
        var act = async () => await _sut.CreateAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidPriceStructureException>();

        _mockCommitmentRepository.Verify(x => x.AddAsync(It.IsAny<Commitment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_GivenAnOverlappingTerm_Throws()
    {
        // Arrange
        _mockCommitmentRepository
            .Setup(x => x.OverlapsAsync(It.IsAny<OrganizationId>(), It.IsAny<EffectiveFrom>(), It.IsAny<EffectiveTo>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        var request = FakeCreateCommitmentRequest.CreateValid(Fixture);

        // Act
        var act = async () => await _sut.CreateAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<CommitmentOverlapException>();

        _mockCommitmentRepository.Verify(x => x.AddAsync(It.IsAny<Commitment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_GivenATermNothingHasDrawnFrom_DeletesIt()
    {
        // Arrange
        var commitment = FakeCommitment.CreateValid(Fixture) with { Drawn = new CommitmentAmount(0) };

        _mockCommitmentRepository
            .Setup(x => x.GetAsync(It.IsAny<CommitmentId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(commitment);

        // Act
        await _sut.DeleteAsync(commitment.CommitmentId, CancellationToken.None);

        // Assert
        _mockCommitmentRepository.Verify(x => x.DeleteAsync(commitment.CommitmentId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_GivenATermAlreadyDrawnFrom_Throws()
    {
        // Arrange
        var commitment = FakeCommitment.CreateValid(Fixture) with { Drawn = new CommitmentAmount(500) };

        _mockCommitmentRepository
            .Setup(x => x.GetAsync(It.IsAny<CommitmentId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(commitment);

        // Act
        var act = async () => await _sut.DeleteAsync(commitment.CommitmentId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<CommitmentInUseException>();

        _mockCommitmentRepository.Verify(x => x.DeleteAsync(It.IsAny<CommitmentId>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
