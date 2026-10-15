using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.Transactions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Transactions;

namespace UnitTests.Tests.Presentation.Validators.Transactions;

public class TransactionRequestValidatorTests : BaseTestClass
{
    private readonly Mock<ITransactionService> _mockTransactionService;
    private readonly TransactionRequestValidator _sut;

    public TransactionRequestValidatorTests()
    {
        _mockTransactionService = new Mock<ITransactionService>();
        _sut = new TransactionRequestValidator(_mockTransactionService.Object);
    }

    [Fact]
    public async Task ValidateAsync_TransactionIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeTransactionRequestDto.CreateValid(Fixture) with
        {
            TransactionId = Guid.Empty
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TransactionId)
            .WithErrorCode("NotEmptyValidator");
    }

    [Fact]
    public async Task ValidateAsync_TransactionDoesNotExist_FailsValidation()
    {
        // Arrange
        var request = FakeTransactionRequestDto.CreateValid(Fixture);

        _mockTransactionService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<TransactionId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TransactionId)
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithErrorMessage("Transaction not found");
    }

    [Fact]
    public async Task ValidateAsync_TransactionExists_SucceedsValidation()
    {
        // Arrange
        var request = FakeTransactionRequestDto.CreateValid(Fixture);

        _mockTransactionService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<TransactionId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();

        _mockTransactionService.Verify(
            x => x.ExistsAsync(
                new OrganizationId(request.OrganizationId),
                new TransactionId(request.TransactionId),
                new IncludeDeleted(false),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
