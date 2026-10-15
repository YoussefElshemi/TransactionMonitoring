using Core.Interfaces.Services;
using Core.Models.Organizations;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.Organizations;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Organizations;

namespace UnitTests.Tests.Presentation.Validators.Organizations;

public class ListOrganizationRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IOrganizationService> _mockOrganizationService;
    private readonly ListOrganizationRequestValidator _sut;

    public ListOrganizationRequestValidatorTests()
    {
        _mockOrganizationService = new Mock<IOrganizationService>();
        _sut = new ListOrganizationRequestValidator(_mockOrganizationService.Object, MockTimeProvider);
    }

    [Fact]
    public async Task ValidateAsync_PageNumberExceedsMaxPageCount_FailsValidation()
    {
        // Arrange
        var request = FakeListOrganizationRequestDto.CreateValid(Fixture) with
        {
            PageSize = 1,
            PageNumber = 2
        };

        _mockOrganizationService
            .Setup(x => x.GetCountAsync(It.IsAny<ListOrganizationRequest>(), It.IsAny<CancellationToken>()))
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
        var request = FakeListOrganizationRequestDto.CreateValid(Fixture) with
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
        var request = FakeListOrganizationRequestDto.CreateValid(Fixture) with
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
        var request = FakeListOrganizationRequestDto.CreateValid(Fixture) with
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
        var request = FakeListOrganizationRequestDto.CreateValid(Fixture) with
        {
            PageNumber = 1,
            PageSize = 10
        };

        _mockOrganizationService
            .Setup(x => x.GetCountAsync(It.IsAny<ListOrganizationRequest>(), It.IsAny<CancellationToken>()))
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