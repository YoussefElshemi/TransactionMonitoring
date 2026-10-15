using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.OrganizationWebhooks;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.OrganizationWebhookConfigurations;

namespace UnitTests.Tests.Presentation.Validators.OrganizationWebhooks;

public class RecoverOrganizationWebhookConfigurationRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IOrganizationService> _mockOrganizationService;
    private readonly Mock<IWebhookConfigurationService> _mockWebhookConfigurationService;
    private readonly RecoverOrganizationWebhookConfigurationRequestValidator _sut;

    public RecoverOrganizationWebhookConfigurationRequestValidatorTests()
    {
        _mockOrganizationService = new Mock<IOrganizationService>();
        _mockWebhookConfigurationService = new Mock<IWebhookConfigurationService>();
        _sut = new RecoverOrganizationWebhookConfigurationRequestValidator(
            _mockOrganizationService.Object,
            _mockWebhookConfigurationService.Object
        );
    }

    [Fact]
    public async Task ValidateAsync_OrganizationIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeRecoverOrganizationWebhookConfigurationRequestDto.CreateValid(Fixture) with
        {
            OrganizationId = Guid.Empty
        };

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
        var request = FakeRecoverOrganizationWebhookConfigurationRequestDto.CreateValid(Fixture);

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
    public async Task ValidateAsync_OrganizationExists_SucceedsValidation()
    {
        // Arrange
        var request = FakeRecoverOrganizationWebhookConfigurationRequestDto.CreateValid(Fixture);

        _mockOrganizationService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.OrganizationId);
    }

    [Fact]
    public async Task ValidateAsync_WebhookConfigurationIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeRecoverOrganizationWebhookConfigurationRequestDto.CreateValid(Fixture) with
        {
            WebhookConfigurationId = Guid.Empty
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WebhookConfigurationId)
            .WithErrorCode("NotEmptyValidator");
    }

    [Fact]
    public async Task ValidateAsync_WebhookConfigurationDoesNotExist_FailsValidation()
    {
        // Arrange
        var request = FakeRecoverOrganizationWebhookConfigurationRequestDto.CreateValid(Fixture);

        _mockWebhookConfigurationService
            .Setup(x => x.ExistsAsync(
                It.IsAny<OrganizationId>(),
                It.IsAny<WebhookConfigurationId>(),
                It.IsAny<IncludeDeleted>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WebhookConfigurationId)
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithErrorMessage("Webhook Configuration not found");
    }

    [Fact]
    public async Task ValidateAsync_WebhookConfigurationExists_SucceedsValidation()
    {
        // Arrange
        var request = FakeRecoverOrganizationWebhookConfigurationRequestDto.CreateValid(Fixture);

        _mockWebhookConfigurationService
            .Setup(x => x.ExistsAsync(
                It.IsAny<OrganizationId>(),
                It.IsAny<WebhookConfigurationId>(),
                It.IsAny<IncludeDeleted>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.WebhookConfigurationId);
    }
}