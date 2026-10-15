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

public class AddUserRoleRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IApplicationUserService> _mockApplicationUserService;
    private readonly AddUserRoleRequestValidator _sut;

    public AddUserRoleRequestValidatorTests()
    {
        _mockApplicationUserService = new Mock<IApplicationUserService>();
        _sut = new AddUserRoleRequestValidator(_mockApplicationUserService.Object);
    }

    [Fact]
    public async Task ValidateAsync_UserIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeAddUserRoleRequestDto.CreateValid(Fixture) with
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
        var request = FakeAddUserRoleRequestDto.CreateValid(Fixture);

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
    public async Task ValidateAsync_UserAlreadyHasRole_FailsValidation()
    {
        // Arrange
        var request = FakeAddUserRoleRequestDto.CreateValid(Fixture) with
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
            .WithErrorCode(HttpStatusCode.Conflict.ToString())
            .WithErrorMessage("The user already has this role");
    }

    [Fact]
    public async Task ValidateAsync_ValidRequest_SucceedsValidation()
    {
        // Arrange
        var request = FakeAddUserRoleRequestDto.CreateValid(Fixture) with
        {
            Role = UserRole.User.ToString()
        };

        var user = FakeApplicationUser.CreateValid(Fixture) with
        {
            Roles = [UserRole.Admin],
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