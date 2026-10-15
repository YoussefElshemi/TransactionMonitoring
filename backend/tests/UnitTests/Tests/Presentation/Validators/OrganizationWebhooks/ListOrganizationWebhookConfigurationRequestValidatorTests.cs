using System.Net;
using Core.Interfaces.Services;
using Core.Models.WebhookConfigurations;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.OrganizationWebhooks;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.OrganizationWebhookConfigurations;

namespace UnitTests.Tests.Presentation.Validators.OrganizationWebhooks;

public class ListOrganizationWebhookConfigurationRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IOrganizationService> _mockOrganizationService;
    private readonly Mock<IWebhookConfigurationService> _mockWebhookConfigurationService;
    private readonly ListOrganizationWebhookConfigurationRequestValidator _sut;

    public ListOrganizationWebhookConfigurationRequestValidatorTests()
    {
        _mockWebhookConfigurationService = new Mock<IWebhookConfigurationService>();
        _mockOrganizationService = new Mock<IOrganizationService>();
        _sut = new ListOrganizationWebhookConfigurationRequestValidator(_mockWebhookConfigurationService.Object, _mockOrganizationService.Object, MockTimeProvider);
    }

    [Fact]
    public async Task ValidateAsync_OrganizationIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeListOrganizationWebhookConfigurationRequestDto.CreateValid(Fixture) with
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
        var request = FakeListOrganizationWebhookConfigurationRequestDto.CreateValid(Fixture);

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
        var request = FakeListOrganizationWebhookConfigurationRequestDto.CreateValid(Fixture);

        _mockOrganizationService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.OrganizationId);
    }

    [Fact]
    public async Task ValidateAsync_InvalidPagedRequest_FailsValidation()
    {
        // Arrange
        var request = FakeListOrganizationWebhookConfigurationRequestDto.CreateValid(Fixture) with
        {
            PageNumber = 0 // Invalid: Less than 1
        };

        _mockOrganizationService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageNumber)
            .WithErrorCode("GreaterThanOrEqualValidator");
    }

    [Fact]
    public async Task ValidateAsync_ValidPagedRequest_SucceedsValidation()
    {
        // Arrange
        var request = FakeListOrganizationWebhookConfigurationRequestDto.CreateValid(Fixture);

        _mockWebhookConfigurationService
            .Setup(x => x.GetCountAsync(It.IsAny<ListOrganizationWebhookConfigurationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TotalCount(10));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PageNumber);
        result.ShouldNotHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public async Task ValidateAsync_PageNumberExceedsMaxPage_FailsValidation()
    {
        // Arrange
        var request = FakeListOrganizationWebhookConfigurationRequestDto.CreateValid(Fixture) with
        {
            PageNumber = 5,
            PageSize = 10
        };

        _mockOrganizationService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockWebhookConfigurationService
            .Setup(x => x.GetCountAsync(It.IsAny<ListOrganizationWebhookConfigurationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TotalCount(20));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageNumber)
            .WithErrorCode("LessThanOrEqualValidator");
    }
}