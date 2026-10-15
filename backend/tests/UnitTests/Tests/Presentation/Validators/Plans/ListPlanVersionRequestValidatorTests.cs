using System.Net;
using Core.Interfaces.Services;
using Core.Models.Plans;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.Plans;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Plans;

namespace UnitTests.Tests.Presentation.Validators.Plans;

public class ListPlanVersionRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IPlanService> _mockPlanService = new();
    private readonly ListPlanVersionRequestValidator _sut;

    public ListPlanVersionRequestValidatorTests()
    {
        _mockPlanService
            .Setup(x => x.GetVersionCountAsync(It.IsAny<ListPlanVersionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TotalCount(100));

        _sut = new ListPlanVersionRequestValidator(_mockPlanService.Object, MockTimeProvider);
    }

    [Fact]
    public async Task ValidateAsync_PlanIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeListPlanVersionRequestDto.CreateValid(Fixture) with { PlanId = Guid.Empty };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PlanId)
            .WithErrorCode("NotEmptyValidator");
    }

    [Fact]
    public async Task ValidateAsync_PlanDoesNotExist_FailsValidation()
    {
        // Arrange
        var request = FakeListPlanVersionRequestDto.CreateValid(Fixture);

        _mockPlanService
            .Setup(x => x.ExistsAsync(It.IsAny<PlanId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PlanId)
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithErrorMessage("Plan not found");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ValidateAsync_PageNumberIsBelowOne_FailsValidation(int pageNumber)
    {
        // Arrange
        var request = FakeListPlanVersionRequestDto.CreateValid(Fixture) with { PageNumber = pageNumber };

        _mockPlanService
            .Setup(x => x.ExistsAsync(It.IsAny<PlanId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageNumber);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task ValidateAsync_PageSizeIsOutOfBounds_FailsValidation(int pageSize)
    {
        // Arrange
        var request = FakeListPlanVersionRequestDto.CreateValid(Fixture) with { PageSize = pageSize };

        _mockPlanService
            .Setup(x => x.ExistsAsync(It.IsAny<PlanId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public async Task ValidateAsync_PlanExists_SucceedsValidation()
    {
        // Arrange
        var request = FakeListPlanVersionRequestDto.CreateValid(Fixture);

        _mockPlanService
            .Setup(x => x.ExistsAsync(It.IsAny<PlanId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
