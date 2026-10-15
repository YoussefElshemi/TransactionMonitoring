using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.Plans;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Plans;

namespace UnitTests.Tests.Presentation.Validators.Plans;

public class UpdatePlanRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IPlanService> _mockPlanService = new();
    private readonly UpdatePlanRequestValidator _sut;

    public UpdatePlanRequestValidatorTests()
    {
        _mockPlanService
            .Setup(x => x.ExistsAsync(It.IsAny<PlanId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _sut = new UpdatePlanRequestValidator(_mockPlanService.Object);
    }

    [Fact]
    public async Task ValidateAsync_PlanIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeUpdatePlanRequestDto.CreateValid(Fixture) with { PlanId = Guid.Empty };

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
        var request = FakeUpdatePlanRequestDto.CreateValid(Fixture);

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
    public async Task ValidateAsync_NameExceedsMaxLength_FailsValidation()
    {
        // Arrange
        var request = FakeUpdatePlanRequestDto.CreateValid(Fixture);
        request = request with { Body = request.Body with { Name = new string('a', PlanName.MaxLength + 1) } };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.Name)
            .WithErrorCode("MaximumLengthValidator");
    }

    [Fact]
    public async Task ValidateAsync_DescriptionExceedsMaxLength_FailsValidation()
    {
        // Arrange
        var request = FakeUpdatePlanRequestDto.CreateValid(Fixture);
        request = request with { Body = request.Body with { Description = new string('a', PlanDescription.MaxLength + 1) } };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.Description)
            .WithErrorCode("MaximumLengthValidator");
    }

    [Fact]
    public async Task ValidateAsync_NameAndDescriptionOmitted_SucceedsValidation()
    {
        // Arrange
        var request = FakeUpdatePlanRequestDto.CreateValid(Fixture);
        request = request with { Body = request.Body with { Name = null, Description = null } };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task ValidateAsync_GivenAValidRequest_SucceedsValidation()
    {
        // Arrange
        var request = FakeUpdatePlanRequestDto.CreateValid(Fixture);

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
