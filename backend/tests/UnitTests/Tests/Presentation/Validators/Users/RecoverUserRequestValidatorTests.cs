using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.Users;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Users;

namespace UnitTests.Tests.Presentation.Validators.Users;

public class RecoverUserRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IApplicationUserService> _mockApplicationUserService;
    private readonly RecoverUserRequestValidator _sut;

    public RecoverUserRequestValidatorTests()
    {
        _mockApplicationUserService = new Mock<IApplicationUserService>();
        _sut = new RecoverUserRequestValidator(_mockApplicationUserService.Object);
    }

    [Fact]
    public async Task ValidateAsync_UserIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeRecoverUserRequestDto.CreateValid(Fixture) with
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
        var request = FakeRecoverUserRequestDto.CreateValid(Fixture);

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
    public async Task ValidateAsync_UserIdExists_SucceedsValidation()
    {
        // Arrange
        var request = FakeRecoverUserRequestDto.CreateValid(Fixture);

        _mockApplicationUserService
            .Setup(x => x.ExistsAsync(It.IsAny<ApplicationUserId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.UserId);
    }
}