using Core.Exceptions;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Models.Adjustments;
using Core.Services;
using Core.ValueObjects;
using FluentAssertions;
using Moq;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Adjustments;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.Tests.Core.Services;

public class InvoiceAdjustmentServiceTests : BaseTestClass
{
    private readonly Mock<IInvoiceAdjustmentRepository> _mockInvoiceAdjustmentRepository = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly InvoiceAdjustmentService _sut;

    public InvoiceAdjustmentServiceTests()
    {
        _mockInvoiceAdjustmentRepository
            .Setup(x => x.GetAsync(It.IsAny<InvoiceAdjustmentId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeInvoiceAdjustment.CreateValid(Fixture));

        _sut = new InvoiceAdjustmentService(_mockInvoiceAdjustmentRepository.Object, _mockUnitOfWork.Object, MockTimeProvider);
    }

    [Fact]
    public async Task CreateAsync_GivenAValidAdjustment_WritesItUnappliedAndSaves()
    {
        // Arrange
        var request = FakeCreateInvoiceAdjustmentRequest.CreateValid(Fixture) with
        {
            Amount = new AdjustmentAmount(-25m)
        };

        // Act
        await _sut.CreateAsync(request, CancellationToken.None);

        // Assert
        _mockInvoiceAdjustmentRepository.Verify(
            x => x.AddAsync(It.Is<InvoiceAdjustment>(a => a.AppliedToInvoiceId == null), It.IsAny<CancellationToken>()),
            Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_GivenAnAmountOfNothing_Throws()
    {
        // Arrange
        var request = FakeCreateInvoiceAdjustmentRequest.CreateValid(Fixture) with
        {
            Amount = new AdjustmentAmount(0)
        };

        // Act
        var act = async () => await _sut.CreateAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidPriceStructureException>();

        _mockInvoiceAdjustmentRepository.Verify(x => x.AddAsync(It.IsAny<InvoiceAdjustment>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_GivenOneNoInvoiceHasCarried_DeletesIt()
    {
        // Arrange
        var adjustment = FakeInvoiceAdjustment.CreateValid(Fixture) with { AppliedToInvoiceId = null };

        _mockInvoiceAdjustmentRepository
            .Setup(x => x.GetAsync(It.IsAny<InvoiceAdjustmentId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(adjustment);

        // Act
        await _sut.DeleteAsync(adjustment.InvoiceAdjustmentId, CancellationToken.None);

        // Assert
        _mockInvoiceAdjustmentRepository.Verify(
            x => x.DeleteAsync(adjustment.InvoiceAdjustmentId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_GivenOneAlreadyCarriedOntoAnInvoice_Throws()
    {
        // Arrange
        var adjustment = FakeInvoiceAdjustment.CreateValid(Fixture) with
        {
            AppliedToInvoiceId = FakeInvoiceId.CreateValid(Fixture)
        };

        _mockInvoiceAdjustmentRepository
            .Setup(x => x.GetAsync(It.IsAny<InvoiceAdjustmentId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(adjustment);

        // Act
        var act = async () => await _sut.DeleteAsync(adjustment.InvoiceAdjustmentId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvoiceAdjustmentAppliedException>();

        _mockInvoiceAdjustmentRepository.Verify(
            x => x.DeleteAsync(It.IsAny<InvoiceAdjustmentId>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
