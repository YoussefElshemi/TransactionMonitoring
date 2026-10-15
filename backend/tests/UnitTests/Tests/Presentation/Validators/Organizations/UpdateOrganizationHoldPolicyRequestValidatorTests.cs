using System.Net;
using AutoFixture;
using Core.Interfaces.Services;
using Core.Models.Common;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Models.Organizations;
using Presentation.Validators.Organizations;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Organizations;

namespace UnitTests.Tests.Presentation.Validators.Organizations;

public class UpdateOrganizationHoldPolicyRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IOrganizationService> _mockOrganizationService;
    private readonly UpdateOrganizationHoldPolicyRequestValidator _sut;

    public UpdateOrganizationHoldPolicyRequestValidatorTests()
    {
        _mockOrganizationService = new Mock<IOrganizationService>();
        _sut = new UpdateOrganizationHoldPolicyRequestValidator(_mockOrganizationService.Object);
    }

    [Fact]
    public async Task ValidateAsync_OrganizationIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeUpdateOrganizationHoldPolicyRequestDto.CreateValid(Fixture) with
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
        var request = FakeUpdateOrganizationHoldPolicyRequestDto.CreateValid(Fixture);

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
        var request = FakeUpdateOrganizationHoldPolicyRequestDto.CreateValid(Fixture);

        _mockOrganizationService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.OrganizationId);
    }

    [Fact]
    public async Task ValidateAsync_HoldWindowIsNotGreaterThanZero_FailsValidation()
    {
        // Arrange
        var request = FakeUpdateOrganizationHoldPolicyRequestDto.CreateValid(Fixture);
        request = request with
        {
            Body = request.Body with
            {
                HoldWindow = Patch<int?>.Set(0)
            }
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.HoldWindow)
            .WithErrorMessage("'Hold Window' must be greater than '0'.");
    }

    [Fact]
    public async Task ValidateAsync_HoldWindowIsExplicitlyNull_SucceedsValidation()
    {
        // Arrange
        var request = FakeUpdateOrganizationHoldPolicyRequestDto.CreateValid(Fixture);
        request = request with
        {
            Body = request.Body with
            {
                HoldWindow = Patch<int?>.Set(null)
            }
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Body.HoldWindow);
    }

    [Fact]
    public async Task ValidateAsync_HoldWindowIsOmitted_SucceedsValidation()
    {
        // Arrange
        var request = FakeUpdateOrganizationHoldPolicyRequestDto.CreateValid(Fixture);
        request = request with
        {
            Body = new UpdateOrganizationHoldPolicyRequestBodyDto()
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Body.HoldWindow);
    }

    [Fact]
    public async Task ValidateAsync_InvalidHoldExpiryVerdict_FailsValidation()
    {
        // Arrange
        var request = FakeUpdateOrganizationHoldPolicyRequestDto.CreateValid(Fixture);
        request = request with
        {
            Body = request.Body with
            {
                HoldExpiryVerdict = Patch<string?>.Set(Fixture.Create<string>())
            }
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.HoldExpiryVerdict);
    }

    [Fact]
    public async Task ValidateAsync_HoldExpiryVerdictIsExplicitlyNull_SucceedsValidation()
    {
        // Arrange
        var request = FakeUpdateOrganizationHoldPolicyRequestDto.CreateValid(Fixture);
        request = request with
        {
            Body = request.Body with
            {
                HoldExpiryVerdict = Patch<string?>.Set(null)
            }
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Body.HoldExpiryVerdict);
    }

    [Fact]
    public async Task ValidateAsync_ValidHoldPolicy_SucceedsValidation()
    {
        // Arrange
        var request = FakeUpdateOrganizationHoldPolicyRequestDto.CreateValid(Fixture);

        _mockOrganizationService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
