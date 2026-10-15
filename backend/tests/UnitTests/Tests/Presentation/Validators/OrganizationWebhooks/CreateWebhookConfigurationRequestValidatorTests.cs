using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.OrganizationWebhooks;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.OrganizationWebhookConfigurations;

namespace UnitTests.Tests.Presentation.Validators.OrganizationWebhooks;

public class CreateWebhookConfigurationRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IOrganizationService> _mockOrganizationService;
    private readonly CreateWebhookConfigurationRequestValidator _sut;

    public CreateWebhookConfigurationRequestValidatorTests()
    {
        _mockOrganizationService = new Mock<IOrganizationService>();
        _sut = new CreateWebhookConfigurationRequestValidator(_mockOrganizationService.Object);
    }

    [Fact]
    public async Task ValidateAsync_OrganizationIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeCreateWebhookConfigurationRequestDto.CreateValid(Fixture) with
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
        var request = FakeCreateWebhookConfigurationRequestDto.CreateValid(Fixture);

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
        var request = FakeCreateWebhookConfigurationRequestDto.CreateValid(Fixture);

        _mockOrganizationService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.OrganizationId);
    }

    [Fact]
    public async Task ValidateAsync_EndpointUrlIsInvalid_FailsValidation()
    {
        // Arrange
        var request = FakeCreateWebhookConfigurationRequestDto.CreateValid(Fixture);
        request = request with
        {
            Body = request.Body with
            {
                EndpointUrl = "invalid-url"
            }
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.EndpointUrl)
            .WithErrorMessage("EndpointUrl must be a valid URI.");
    }

    [Fact]
    public async Task ValidateAsync_EndpointUrlIsValid_SucceedsValidation()
    {
        // Arrange
        var request = FakeCreateWebhookConfigurationRequestDto.CreateValid(Fixture);

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Body.EndpointUrl);
    }

    [Fact]
    public async Task ValidateAsync_IsEnabledIsNotNull_SucceedsValidation()
    {
        // Arrange
        var request = FakeCreateWebhookConfigurationRequestDto.CreateValid(Fixture);

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Body.IsEnabled);
    }
}