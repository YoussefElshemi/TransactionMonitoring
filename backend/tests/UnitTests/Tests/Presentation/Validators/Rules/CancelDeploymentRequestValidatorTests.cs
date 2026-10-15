using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.Rules;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Rules;

namespace UnitTests.Tests.Presentation.Validators.Rules;

public class CancelDeploymentRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IRuleService> _mockRuleService = new();
    private readonly Mock<IRuleDeploymentService> _mockRuleDeploymentService = new();
    private readonly CancelDeploymentRequestValidator _sut;

    public CancelDeploymentRequestValidatorTests()
    {
        _mockRuleService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockRuleDeploymentService
            .Setup(x => x.DeploymentInProgressAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleDeploymentId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _sut = new CancelDeploymentRequestValidator(_mockRuleService.Object, _mockRuleDeploymentService.Object);
    }

    [Fact]
    public async Task ValidateAsync_RuleDoesNotExist_FailsValidation()
    {
        // Arrange
        var request = FakeCancelDeploymentRequestDto.CreateValid(Fixture);

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
    public async Task ValidateAsync_NoInProgressDeployment_FailsValidation()
    {
        // Arrange
        var request = FakeCancelDeploymentRequestDto.CreateValid(Fixture);

        _mockRuleDeploymentService
            .Setup(x => x.DeploymentInProgressAsync(It.IsAny<OrganizationId>(), It.IsAny<RuleDeploymentId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DeploymentId)
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithErrorMessage("No in-progress deployment found");
    }

    [Fact]
    public async Task ValidateAsync_ValidRequest_SucceedsValidation()
    {
        // Arrange
        var request = FakeCancelDeploymentRequestDto.CreateValid(Fixture);

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
