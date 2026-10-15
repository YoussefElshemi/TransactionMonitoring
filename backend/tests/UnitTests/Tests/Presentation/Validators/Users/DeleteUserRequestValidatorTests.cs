using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.Users;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Users;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Users;

namespace UnitTests.Tests.Presentation.Validators.Users;

public class DeleteUserRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IApplicationUserService> _mockApplicationUserService;
    private readonly DeleteUserRequestValidator _sut;

    public DeleteUserRequestValidatorTests()
    {
        _mockApplicationUserService = new Mock<IApplicationUserService>();
        _sut = new DeleteUserRequestValidator(_mockApplicationUserService.Object);
    }

    [Fact]
    public async Task ValidateAsync_UserIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeDeleteUserRequestDto.CreateValid(Fixture) with
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
        var request = FakeDeleteUserRequestDto.CreateValid(Fixture);

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
    public async Task ValidateAsync_UserIdDoesExist_SucceedsValidation()
    {
        // Arrange
        var request = FakeDeleteUserRequestDto.CreateValid(Fixture);

        _mockApplicationUserService
            .Setup(x => x.ExistsAsync(It.IsAny<ApplicationUserId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockApplicationUserService
            .Setup(x => x.GetAsync(It.IsAny<ApplicationUserId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeApplicationUser.CreateValid(Fixture));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.UserId);
    }
}