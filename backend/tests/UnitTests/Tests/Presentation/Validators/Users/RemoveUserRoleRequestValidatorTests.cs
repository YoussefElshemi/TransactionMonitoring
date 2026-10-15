using System.Net;
using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.Users;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Users;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Users;

namespace UnitTests.Tests.Presentation.Validators.Users;

public class RemoveUserRoleRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IApplicationUserService> _mockApplicationUserService;
    private readonly RemoveUserRoleRequestValidator _sut;

    public RemoveUserRoleRequestValidatorTests()
    {
        _mockApplicationUserService = new Mock<IApplicationUserService>();
        _sut = new RemoveUserRoleRequestValidator(_mockApplicationUserService.Object);
    }

    [Fact]
    public async Task ValidateAsync_UserIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeRemoveUserRoleRequestDto.CreateValid(Fixture) with
        {
            UserId = Guid.Empty
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorCode("NotEmptyValidator");
    }

    [Fact]
    public async Task ValidateAsync_UserIdDoesNotExist_FailsValidation()
    {
        // Arrange
        var request = FakeRemoveUserRoleRequestDto.CreateValid(Fixture);

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
    public async Task ValidateAsync_UserCannotRemoveAdminRole_FailsValidation()
    {
        // Arrange
        var request = FakeRemoveUserRoleRequestDto.CreateValid(Fixture) with
        {
            Role = UserRole.Admin.ToString()
        };

        var user = FakeApplicationUser.CreateValid(Fixture) with { Roles = [UserRole.Admin] };

        _mockApplicationUserService
            .Setup(x => x.ExistsAsync(It.IsAny<ApplicationUserId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockApplicationUserService
            .Setup(x => x.GetAsync(It.IsAny<ApplicationUserId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveAnyValidationError()
            .WithErrorCode(HttpStatusCode.Forbidden.ToString())
            .WithErrorMessage("You cannot remove the admin role from this user.");
    }

    [Fact]
    public async Task ValidateAsync_UserDoesNotHaveRole_FailsValidation()
    {
        // Arrange
        var request = FakeRemoveUserRoleRequestDto.CreateValid(Fixture) with
        {
            Role = UserRole.Admin.ToString()
        };

        var user = FakeApplicationUser.CreateValid(Fixture) with { Roles = [UserRole.User] };

        _mockApplicationUserService
            .Setup(x => x.ExistsAsync(It.IsAny<ApplicationUserId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockApplicationUserService
            .Setup(x => x.GetAsync(It.IsAny<ApplicationUserId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveAnyValidationError()
            .WithErrorMessage("The user does not have this role");
    }

    [Fact]
    public async Task ValidateAsync_ValidRequest_SucceedsValidation()
    {
        // Arrange
        var request = FakeRemoveUserRoleRequestDto.CreateValid(Fixture) with
        {
            Role = UserRole.User.ToString()
        };

        var user = FakeApplicationUser.CreateValid(Fixture) with
        {
            Roles = [UserRole.Admin, UserRole.User],
        };

        _mockApplicationUserService
            .Setup(x => x.GetAsync(It.IsAny<ApplicationUserId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockApplicationUserService
            .Setup(x => x.ExistsAsync(It.IsAny<ApplicationUserId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.UserId);
        result.ShouldNotHaveValidationErrorFor(x => x.Role);
    }
}