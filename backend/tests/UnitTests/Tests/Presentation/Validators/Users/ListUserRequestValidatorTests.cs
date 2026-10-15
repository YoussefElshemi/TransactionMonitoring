using Core.Interfaces.Services;
using Core.Models.Users;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.Users;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Users;

namespace UnitTests.Tests.Presentation.Validators.Users;

public class ListUserRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IApplicationUserService> _mockApplicationUserService;
    private readonly ListUserRequestValidator _sut;

    public ListUserRequestValidatorTests()
    {
        _mockApplicationUserService = new Mock<IApplicationUserService>();
        _sut = new ListUserRequestValidator(_mockApplicationUserService.Object, MockTimeProvider);
    }

    [Fact]
    public async Task ValidateAsync_PageNumberExceedsMaxPageCount_FailsValidation()
    {
        // Arrange
        var request = FakeListUserRequestDto.CreateValid(Fixture) with
        {
            PageSize = 1,
            PageNumber = 2,
            FromDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime.AddDays(-1)),
            ToDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime)
        };

        _mockApplicationUserService
            .Setup(x => x.GetCountAsync(It.IsAny<ListUserRequest>(), It.IsAny<CancellationToken>()))
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
        var request = FakeListUserRequestDto.CreateValid(Fixture) with
        {
            PageSize = 200,
            FromDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime.AddDays(-1)),
            ToDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime)
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
        var request = FakeListUserRequestDto.CreateValid(Fixture) with
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
        var request = FakeListUserRequestDto.CreateValid(Fixture) with
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
        var request = FakeListUserRequestDto.CreateValid(Fixture) with
        {
            PageNumber = 1,
            PageSize = 10,
            FromDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime.AddDays(-1)),
            ToDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime)
        };

        _mockApplicationUserService
            .Setup(x => x.GetCountAsync(It.IsAny<ListUserRequest>(), It.IsAny<CancellationToken>()))
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