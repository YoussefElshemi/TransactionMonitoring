using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.OrganizationUsers;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.OrganizationUsers;

namespace UnitTests.Tests.Presentation.Validators.OrganizationUsers;

public class RemoveOrganizationUserRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IOrganizationService> _mockOrganizationService;
    private readonly Mock<IOrganizationUserService> _mockOrganizationUserService;
    private readonly RemoveOrganizationUserRequestValidator _sut;

    public RemoveOrganizationUserRequestValidatorTests()
    {
        _mockOrganizationUserService = new Mock<IOrganizationUserService>();
        _mockOrganizationService = new Mock<IOrganizationService>();
        _sut = new RemoveOrganizationUserRequestValidator(
            _mockOrganizationService.Object,
            _mockOrganizationUserService.Object);
    }

    [Fact]
    public async Task ValidateAsync_OrganizationIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeRemoveOrganizationUserRequestDto.CreateValid(Fixture) with
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
        var request = FakeRemoveOrganizationUserRequestDto.CreateValid(Fixture);

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
    public async Task ValidateAsync_OrganizationUserIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeRemoveOrganizationUserRequestDto.CreateValid(Fixture) with
        {
            UserId = Guid.Empty
        };

        _mockOrganizationService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorCode("NotEmptyValidator");
    }

    [Fact]
    public async Task ValidateAsync_OrganizationUserIdDoesNotExist_FailsValidation()
    {
        // Arrange
        var request = FakeRemoveOrganizationUserRequestDto.CreateValid(Fixture);

        _mockOrganizationService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockOrganizationUserService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<ApplicationUserId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithErrorMessage("User not found");
    }

    [Fact]
    public async Task ValidateAsync_OrganizationUserIdDoesExist_SucceedsValidation()
    {
        // Arrange
        var request = FakeRemoveOrganizationUserRequestDto.CreateValid(Fixture);

        _mockOrganizationService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockOrganizationUserService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<ApplicationUserId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}