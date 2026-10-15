using Core.Interfaces.Services;
using Core.Models.Invoices;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.Invoices;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Invoices;

namespace UnitTests.Tests.Presentation.Validators.Invoices;

public class ListInvoiceRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IInvoiceService> _mockInvoiceService;
    private readonly ListInvoiceRequestValidator _sut;

    public ListInvoiceRequestValidatorTests()
    {
        _mockInvoiceService = new Mock<IInvoiceService>();
        _sut = new ListInvoiceRequestValidator(_mockInvoiceService.Object, MockTimeProvider);
    }

    [Fact]
    public async Task ValidateAsync_PageNumberExceedsMaxPageCount_FailsValidation()
    {
        // Arrange
        var request = FakeListInvoiceRequestDto.CreateValid(Fixture) with
        {
            PageSize = 1,
            PageNumber = 2
        };

        _mockInvoiceService
            .Setup(x => x.GetCountAsync(It.IsAny<ListInvoiceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TotalCount(1));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageNumber)
            .WithErrorCode("LessThanOrEqualValidator");
    }

    [Fact]
    public async Task ValidateAsync_PageSizeExceedsMax_FailsValidation()
    {
        // Arrange
        var request = FakeListInvoiceRequestDto.CreateValid(Fixture) with
        {
            PageSize = 200
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageSize)
            .WithErrorCode("LessThanOrEqualValidator");
    }

    [Fact]
    public async Task ValidateAsync_FromDateInFuture_FailsValidation()
    {
        // Arrange
        var request = FakeListInvoiceRequestDto.CreateValid(Fixture) with
        {
            FromDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime.AddDays(1))
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FromDate)
            .WithErrorCode("LessThanOrEqualValidator");
    }

    [Fact]
    public async Task ValidateAsync_FromDateAfterToDate_FailsValidation()
    {
        // Arrange
        var request = FakeListInvoiceRequestDto.CreateValid(Fixture) with
        {
            FromDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime.AddDays(1))
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FromDate)
            .WithErrorCode("LessThanOrEqualValidator");
    }

    [Fact]
    public async Task ValidateAsync_ValidRequest_SucceedsValidation()
    {
        // Arrange
        var request = FakeListInvoiceRequestDto.CreateValid(Fixture) with
        {
            PageNumber = 1,
            PageSize = 10
        };

        _mockInvoiceService
            .Setup(x => x.GetCountAsync(It.IsAny<ListInvoiceRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TotalCount(100));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PageNumber);
        result.ShouldNotHaveValidationErrorFor(x => x.PageSize);
        result.ShouldNotHaveValidationErrorFor(x => x.FromDate);
        result.ShouldNotHaveValidationErrorFor(x => x.ToDate);
    }
}