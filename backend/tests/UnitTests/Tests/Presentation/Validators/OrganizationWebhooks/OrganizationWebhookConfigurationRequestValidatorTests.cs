using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.OrganizationWebhooks;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.OrganizationWebhookConfigurations;

namespace UnitTests.Tests.Presentation.Validators.OrganizationWebhooks;

public class OrganizationWebhookConfigurationRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IOrganizationService> _mockOrganizationService;
    private readonly Mock<IWebhookConfigurationService> _mockWebhookConfigurationService;
    private readonly OrganizationWebhookConfigurationRequestValidator _sut;

    public OrganizationWebhookConfigurationRequestValidatorTests()
    {
        _mockWebhookConfigurationService = new Mock<IWebhookConfigurationService>();
        _mockOrganizationService = new Mock<IOrganizationService>();
        _sut = new OrganizationWebhookConfigurationRequestValidator(_mockOrganizationService.Object, _mockWebhookConfigurationService.Object);
    }

    [Fact]
    public async Task ValidateAsync_OrganizationIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeOrganizationWebhookConfigurationRequestDto.CreateValid(Fixture) with
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
    public async Task ValidateAsync_OrganizationIdDoesNotExist_FailsValidation()
    {
        // Arrange
        var request = FakeOrganizationWebhookConfigurationRequestDto.CreateValid(Fixture);

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
    public async Task ValidateAsync_OrganizationWebhookConfigurationIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeOrganizationWebhookConfigurationRequestDto.CreateValid(Fixture) with
        {
            WebhookConfigurationId = Guid.Empty
        };

        _mockOrganizationService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WebhookConfigurationId)
            .WithErrorCode("NotEmptyValidator");
    }

    [Fact]
    public async Task ValidateAsync_OrganizationWebhookConfigurationIdDoesNotExist_FailsValidation()
    {
        // Arrange
        var request = FakeOrganizationWebhookConfigurationRequestDto.CreateValid(Fixture);

        _mockOrganizationService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockWebhookConfigurationService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<WebhookConfigurationId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WebhookConfigurationId)
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithErrorMessage("Webhook Configuration not found");
    }

    [Fact]
    public async Task ValidateAsync_OrganizationWebhookConfigurationIdDoesExist_SucceedsValidation()
    {
        // Arrange
        var request = FakeOrganizationWebhookConfigurationRequestDto.CreateValid(Fixture);

        _mockOrganizationService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockWebhookConfigurationService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<WebhookConfigurationId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}