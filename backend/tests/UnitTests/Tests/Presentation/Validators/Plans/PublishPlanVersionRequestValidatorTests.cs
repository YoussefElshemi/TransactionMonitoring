using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.Plans;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Plans;

namespace UnitTests.Tests.Presentation.Validators.Plans;

public class PublishPlanVersionRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IPlanService> _mockPlanService = new();
    private readonly PublishPlanVersionRequestValidator _sut;

    public PublishPlanVersionRequestValidatorTests()
    {
        _mockPlanService
            .Setup(x => x.ExistsAsync(It.IsAny<PlanId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _sut = new PublishPlanVersionRequestValidator(_mockPlanService.Object, new PlanPriceRequestValidator());
    }

    [Fact]
    public async Task ValidateAsync_PlanIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakePublishPlanVersionRequestDto.CreateValid(Fixture) with { PlanId = Guid.Empty };

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
        var request = FakePublishPlanVersionRequestDto.CreateValid(Fixture);

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
    public async Task ValidateAsync_CostPerRequestIsNegative_FailsValidation()
    {
        // Arrange
        var request = FakePublishPlanVersionRequestDto.CreateValid(Fixture);
        request = request with { Body = request.Body with { CostPerRequest = -0.01m } };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.CostPerRequest)
            .WithErrorCode("GreaterThanOrEqualValidator");
    }

    [Fact]
    public async Task ValidateAsync_CostPerRequestIsZero_SucceedsValidation()
    {
        // Arrange
        var request = FakePublishPlanVersionRequestDto.CreateValid(Fixture);
        request = request with { Body = request.Body with { CostPerRequest = 0 } };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task ValidateAsync_GivenAValidRequest_SucceedsValidation()
    {
        // Arrange
        var request = FakePublishPlanVersionRequestDto.CreateValid(Fixture);

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
