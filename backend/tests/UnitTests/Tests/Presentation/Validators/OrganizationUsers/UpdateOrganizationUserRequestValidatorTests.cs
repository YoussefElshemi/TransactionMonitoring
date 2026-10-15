using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.OrganizationUsers;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Users;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.OrganizationUsers;

namespace UnitTests.Tests.Presentation.Validators.OrganizationUsers;

public class UpdateOrganizationUserRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IApplicationUserService> _mockApplicationUserService;
    private readonly Mock<IOrganizationService> _mockOrganizationService;
    private readonly UpdateOrganizationUserRequestValidator _sut;

    public UpdateOrganizationUserRequestValidatorTests()
    {
        _mockApplicationUserService = new Mock<IApplicationUserService>();
        _mockOrganizationService = new Mock<IOrganizationService>();
        _sut = new UpdateOrganizationUserRequestValidator(_mockOrganizationService.Object, _mockApplicationUserService.Object);
    }

    [Fact]
    public async Task ValidateAsync_OrganizationIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeUpdateOrganizationUserRequestDto.CreateValid(Fixture) with
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
        var request = FakeUpdateOrganizationUserRequestDto.CreateValid(Fixture);

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
        var request = FakeUpdateOrganizationUserRequestDto.CreateValid(Fixture) with
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
        var request = FakeUpdateOrganizationUserRequestDto.CreateValid(Fixture);

        _mockOrganizationService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockApplicationUserService
            .Setup(x => x.ExistsAsync(It.IsAny<ApplicationUserId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
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
        var request = FakeUpdateOrganizationUserRequestDto.CreateValid(Fixture);

        _mockOrganizationService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockApplicationUserService
            .Setup(x => x.ExistsAsync(It.IsAny<ApplicationUserId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockApplicationUserService
            .Setup(x => x.GetAsync(It.IsAny<ApplicationUserId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeApplicationUser.CreateValid(Fixture));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}