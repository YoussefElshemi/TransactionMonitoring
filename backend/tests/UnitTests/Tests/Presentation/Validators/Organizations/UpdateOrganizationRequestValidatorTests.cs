using System.Net;
using AutoFixture;
using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.Organizations;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Organizations;

namespace UnitTests.Tests.Presentation.Validators.Organizations;

public class UpdateOrganizationRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IOrganizationService> _mockOrganizationService;
    private readonly UpdateOrganizationRequestValidator _sut;

    public UpdateOrganizationRequestValidatorTests()
    {
        _mockOrganizationService = new Mock<IOrganizationService>();
        _sut = new UpdateOrganizationRequestValidator(_mockOrganizationService.Object);
    }

    [Fact]
    public async Task ValidateAsync_OrganizationIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeUpdateOrganizationRequestDto.CreateValid(Fixture) with
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
        var request = FakeUpdateOrganizationRequestDto.CreateValid(Fixture);

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
        var request = FakeUpdateOrganizationRequestDto.CreateValid(Fixture);

        _mockOrganizationService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.OrganizationId);
    }

    [Fact]
    public async Task ValidateAsync_FriendlyNameIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeUpdateOrganizationRequestDto.CreateValid(Fixture);
        request = request with
        {
            Body = request.Body with
            {
                FriendlyName = string.Empty
            }
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.FriendlyName)
            .WithErrorCode("NotEmptyValidator");
    }

    [Fact]
    public async Task ValidateAsync_FriendlyNameExceedsMaxLength_FailsValidation()
    {
        // Arrange
        var request = FakeUpdateOrganizationRequestDto.CreateValid(Fixture);
        request = request with
        {
            Body = request.Body with
            {
                FriendlyName = new string('A', 51)
            }
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.FriendlyName)
            .WithErrorCode("MaximumLengthValidator");
    }

    [Fact]
    public async Task ValidateAsync_FriendlyNameIsValid_SucceedsValidation()
    {
        // Arrange
        var request = FakeUpdateOrganizationRequestDto.CreateValid(Fixture);

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Body.FriendlyName);
    }

    [Fact]
    public async Task ValidateAsync_InvalidStatus_FailsValidation()
    {
        // Arrange
        var request = FakeUpdateOrganizationRequestDto.CreateValid(Fixture);
        request = request with
        {
            Body = request.Body with
            {
                Status = Fixture.Create<string>()
            }
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.Status)
            .WithErrorCode("StringEnumValidator");
    }

    [Fact]
    public async Task ValidateAsync_StatusNotEnabledOrDisabled_FailsValidation()
    {
        // Arrange
        var request = FakeUpdateOrganizationRequestDto.CreateValid(Fixture);
        request = request with
        {
            Body = request.Body with
            {
                Status = OrganizationStatus.OverdueInvoice.ToString()
            }
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.Status)
            .WithErrorMessage("Status must be one of 'Enabled', 'Disabled'");
    }
}
