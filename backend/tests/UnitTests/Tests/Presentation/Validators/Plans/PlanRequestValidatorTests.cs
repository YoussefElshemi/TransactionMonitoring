using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.Plans;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Plans;

namespace UnitTests.Tests.Presentation.Validators.Plans;

public class PlanRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IPlanService> _mockPlanService = new();
    private readonly PlanRequestValidator _sut;

    public PlanRequestValidatorTests()
    {
        _sut = new PlanRequestValidator(_mockPlanService.Object);
    }

    [Fact]
    public async Task ValidateAsync_PlanIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakePlanRequestDto.CreateValid(Fixture) with { PlanId = Guid.Empty };

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
        var request = FakePlanRequestDto.CreateValid(Fixture);

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

    [Fact]
    public async Task ValidateAsync_PlanExists_SucceedsValidation()
    {
        // Arrange
        var request = FakePlanRequestDto.CreateValid(Fixture);

        _mockPlanService
            .Setup(x => x.ExistsAsync(It.IsAny<PlanId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
