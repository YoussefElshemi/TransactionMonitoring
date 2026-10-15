using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.Alerts;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.Alerts;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Alerts;

namespace UnitTests.Tests.Presentation.Validators.Alerts;

public class ListAlertRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IAlertService> _mockAlertService = new();
    private readonly ListAlertRequestValidator _sut;

    public ListAlertRequestValidatorTests()
    {
        _mockAlertService
            .Setup(x => x.GetCountAsync(It.IsAny<ListAlertRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TotalCount(100));

        _sut = new ListAlertRequestValidator(_mockAlertService.Object, MockTimeProvider);
    }

    [Fact]
    public async Task ValidateAsync_InvalidStatus_FailsValidation()
    {
        // Arrange
        var request = FakeListAlertRequestDto.CreateValid(Fixture) with
        {
            Status = "Invalid"
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Status)
            .WithErrorCode("StringEnumValidator");
    }

    [Theory]
    [InlineData("Open")]
    [InlineData("open")]
    [InlineData("SUPPRESSED")]
    public async Task ValidateAsync_AnyCasingOfAKnownStatus_SucceedsValidation(string status)
    {
        // Arrange
        var request = FakeListAlertRequestDto.CreateValid(Fixture) with
        {
            Status = status
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public async Task ValidateAsync_InvalidSeverity_FailsValidation()
    {
        // Arrange
        var request = FakeListAlertRequestDto.CreateValid(Fixture) with
        {
            Severity = "Invalid"
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Severity)
            .WithErrorCode("StringEnumValidator");
    }

    [Fact]
    public async Task ValidateAsync_ValidSeverity_SucceedsValidation()
    {
        // Arrange
        var request = FakeListAlertRequestDto.CreateValid(Fixture) with
        {
            Severity = nameof(RuleSeverity.Critical)
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Severity);
    }

    [Fact]
    public async Task ValidateAsync_NoStatusOrSeverity_SucceedsValidation()
    {
        // Arrange
        var request = FakeListAlertRequestDto.CreateValid(Fixture) with
        {
            Status = null,
            Severity = null
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Status);
        result.ShouldNotHaveValidationErrorFor(x => x.Severity);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ValidateAsync_PageNumberBelowOne_FailsValidation(int pageNumber)
    {
        // Arrange
        var request = FakeListAlertRequestDto.CreateValid(Fixture) with
        {
            PageNumber = pageNumber
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageNumber);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task ValidateAsync_PageSizeOutOfRange_FailsValidation(int pageSize)
    {
        // Arrange
        var request = FakeListAlertRequestDto.CreateValid(Fixture) with
        {
            PageSize = pageSize
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public async Task ValidateAsync_PageNumberPastTheEnd_FailsValidation()
    {
        // Arrange
        _mockAlertService
            .Setup(x => x.GetCountAsync(It.IsAny<ListAlertRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TotalCount(5));

        var request = FakeListAlertRequestDto.CreateValid(Fixture) with
        {
            PageNumber = 99,
            PageSize = 10
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageNumber)
            .WithErrorCode("LessThanOrEqualValidator");
    }

    [Fact]
    public async Task ValidateAsync_FromDateInTheFuture_FailsValidation()
    {
        // Arrange
        var request = FakeListAlertRequestDto.CreateValid(Fixture) with
        {
            FromDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime).AddDays(1)
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FromDate);
    }

    [Fact]
    public async Task ValidateAsync_FromDateAfterToDate_FailsValidation()
    {
        // Arrange
        var request = FakeListAlertRequestDto.CreateValid(Fixture) with
        {
            FromDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime),
            ToDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime).AddDays(-1)
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveAnyValidationError();
    }
}
