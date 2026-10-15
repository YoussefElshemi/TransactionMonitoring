using Core.Interfaces.Services;
using Core.Models.Transactions;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.Transactions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Transactions;

namespace UnitTests.Tests.Presentation.Validators.Transactions;

public class ListTransactionRequestValidatorTests : BaseTestClass
{
    private readonly Mock<ITransactionService> _mockTransactionService;
    private readonly ListTransactionRequestValidator _sut;

    public ListTransactionRequestValidatorTests()
    {
        _mockTransactionService = new Mock<ITransactionService>();
        _sut = new ListTransactionRequestValidator(_mockTransactionService.Object, MockTimeProvider);
    }

    [Fact]
    public async Task ValidateAsync_ValidRequest_SucceedsValidation()
    {
        // Arrange
        var request = FakeListTransactionRequestDto.CreateValid(Fixture) with
        {
            PageNumber = 1,
            PageSize = 10,
            FromDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime.AddDays(-1)),
            ToDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime)
        };

        _mockTransactionService
            .Setup(x => x.GetCountAsync(It.IsAny<ListTransactionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TotalCount(10));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task ValidateAsync_InvalidPagedRequest_FailsValidation()
    {
        // Arrange
        var request = FakeListTransactionRequestDto.CreateValid(Fixture) with
        {
            PageNumber = 0,
            PageSize = 10,
            FromDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime.AddDays(-1)),
            ToDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime)
        };

        _mockTransactionService
            .Setup(x => x.GetCountAsync(It.IsAny<ListTransactionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TotalCount(20));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageNumber)
            .WithErrorCode("GreaterThanOrEqualValidator");
    }

    [Fact]
    public async Task ValidateAsync_PageNumberExceedsMaxPage_FailsValidation()
    {
        // Arrange
        var request = FakeListTransactionRequestDto.CreateValid(Fixture) with
        {
            PageNumber = 5,
            PageSize = 10,
            FromDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime.AddDays(-1)),
            ToDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime)
        };

        _mockTransactionService
            .Setup(x => x.GetCountAsync(It.IsAny<ListTransactionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TotalCount(20));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageNumber)
            .WithErrorCode("LessThanOrEqualValidator");
    }
}
