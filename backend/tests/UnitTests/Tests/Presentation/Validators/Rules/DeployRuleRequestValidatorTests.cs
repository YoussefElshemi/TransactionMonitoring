using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.Rules;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Rules;

namespace UnitTests.Tests.Presentation.Validators.Rules;

public class DeployRuleRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IRuleService> _mockRuleService = new();
    private readonly Mock<IRuleVersionService> _mockRuleVersionService = new();
    private readonly Mock<IRuleDeploymentService> _mockRuleDeploymentService = new();
    private readonly DeployRuleRequestValidator _sut;

    public DeployRuleRequestValidatorTests()
    {
        _mockRuleService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockRuleDeploymentService
            .Setup(x => x.ActiveDeploymentExistsAsync(It.IsAny<RuleId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        _mockRuleVersionService
            .Setup(x => x.VersionExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleId>(), It.IsAny<VersionNumber>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _sut = new DeployRuleRequestValidator(_mockRuleService.Object, _mockRuleVersionService.Object, _mockRuleDeploymentService.Object);
    }

    [Fact]
    public async Task ValidateAsync_RuleDoesNotExist_FailsValidation()
    {
        // Arrange
        var request = FakeDeployRuleRequestDto.CreateValid(Fixture);

        _mockRuleService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RuleId)
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithErrorMessage("Rule not found");
    }

    [Fact]
    public async Task ValidateAsync_ActiveDeploymentAlreadyInProgress_FailsValidation()
    {
        // Arrange
        var request = FakeDeployRuleRequestDto.CreateValid(Fixture);

        _mockRuleDeploymentService
            .Setup(x => x.ActiveDeploymentExistsAsync(It.IsAny<RuleId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.RuleId)
            .WithErrorCode(HttpStatusCode.Conflict.ToString())
            .WithErrorMessage("A deployment is already in progress for this rule");
    }

    [Fact]
    public async Task ValidateAsync_VersionDoesNotExist_FailsValidation()
    {
        // Arrange
        var request = FakeDeployRuleRequestDto.CreateValid(Fixture);

        _mockRuleVersionService
            .Setup(x => x.VersionExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleId>(), It.IsAny<VersionNumber>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.Errors.Should().Contain(x =>
            x.ErrorCode == HttpStatusCode.NotFound.ToString() &&
            x.ErrorMessage == "Rule version not found");
    }

    [Fact]
    public async Task ValidateAsync_GradualWithoutStepPercentage_FailsValidation()
    {
        // Arrange
        var request = FakeDeployRuleRequestDto.CreateValidGradual(Fixture);
        request = request with { Body = request.Body with { StepPercentage = null } };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.StepPercentage)
            .WithErrorMessage("Step percentage is required for a gradual deployment");
    }

    [Fact]
    public async Task ValidateAsync_ValidRequest_SucceedsValidation()
    {
        // Arrange
        var request = FakeDeployRuleRequestDto.CreateValidGradual(Fixture);

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
