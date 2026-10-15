using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.Subscriptions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Subscriptions;

namespace UnitTests.Tests.Presentation.Validators.Subscriptions;

public class CreateSubscriptionRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IOrganizationService> _mockOrganizationService = new();
    private readonly Mock<IPlanService> _mockPlanService = new();
    private readonly CreateSubscriptionRequestValidator _sut;

    public CreateSubscriptionRequestValidatorTests()
    {
        _mockOrganizationService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockPlanService
            .Setup(x => x.VersionExistsAsync(It.IsAny<PlanVersionId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _sut = new CreateSubscriptionRequestValidator(
            _mockOrganizationService.Object, _mockPlanService.Object, MockTimeProvider);
    }

    [Fact]
    public async Task ValidateAsync_OrganizationIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeCreateSubscriptionRequestDto.CreateValid(Fixture) with { OrganizationId = Guid.Empty };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrganizationId)
            .WithErrorCode("NotEmptyValidator");
    }

    [Fact]
    public async Task ValidateAsync_OrganizationDoesNotExist_FailsValidation()
    {
        // Arrange
        var request = FakeCreateSubscriptionRequestDto.CreateValid(Fixture);

        _mockOrganizationService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OrganizationId)
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithErrorMessage("Organization not found");
    }

    [Fact]
    public async Task ValidateAsync_PlanVersionDoesNotExist_FailsValidation()
    {
        // Arrange
        var request = FakeCreateSubscriptionRequestDto.CreateValid(Fixture);

        _mockPlanService
            .Setup(x => x.VersionExistsAsync(It.IsAny<PlanVersionId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.PlanVersionId)
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithErrorMessage("Plan version not found");
    }

    [Fact]
    public async Task ValidateAsync_EffectiveFromIsInThePast_FailsValidation()
    {
        // Arrange
        var request = FakeCreateSubscriptionRequestDto.CreateValid(Fixture);
        request = request with
        {
            Body = request.Body with { EffectiveFrom = MockTimeProvider.GetUtcNow().AddDays(-1) }
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.EffectiveFrom)
            .WithErrorMessage("EffectiveFrom must be in the future. Omit it to start the subscription now.");
    }

    [Fact]
    public async Task ValidateAsync_EffectiveFromIsNow_FailsValidation()
    {
        // Arrange
        var request = FakeCreateSubscriptionRequestDto.CreateValid(Fixture);
        request = request with
        {
            Body = request.Body with { EffectiveFrom = MockTimeProvider.GetUtcNow() }
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.EffectiveFrom);
    }

    [Fact]
    public async Task ValidateAsync_EffectiveFromIsInTheFuture_SucceedsValidation()
    {
        // Arrange
        var request = FakeCreateSubscriptionRequestDto.CreateValid(Fixture);
        request = request with
        {
            Body = request.Body with { EffectiveFrom = MockTimeProvider.GetUtcNow().AddMonths(1) }
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task ValidateAsync_EffectiveFromIsOmitted_SucceedsValidation()
    {
        // Arrange
        var request = FakeCreateSubscriptionRequestDto.CreateValid(Fixture);
        request = request with { Body = request.Body with { EffectiveFrom = null } };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
