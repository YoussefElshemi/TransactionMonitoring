using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.BackTests;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.BackTests;

namespace UnitTests.Tests.Presentation.Validators.BackTests;

public class BackTestRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IBackTestService> _mockBackTestService;
    private readonly BackTestRequestValidator _sut;

    public BackTestRequestValidatorTests()
    {
        _mockBackTestService = new Mock<IBackTestService>();
        _sut = new BackTestRequestValidator(_mockBackTestService.Object);
    }

    [Fact]
    public async Task ValidateAsync_BackTestIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeBackTestRequestDto.CreateValid(Fixture) with
        {
            BackTestId = Guid.Empty
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.BackTestId)
            .WithErrorCode("NotEmptyValidator");
    }

    [Fact]
    public async Task ValidateAsync_BackTestDoesNotExist_FailsValidation()
    {
        // Arrange
        var request = FakeBackTestRequestDto.CreateValid(Fixture);

        _mockBackTestService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<BackTestId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.BackTestId)
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithErrorMessage("BackTest not found");
    }

    [Fact]
    public async Task ValidateAsync_ValidRequest_SucceedsValidation()
    {
        // Arrange
        var request = FakeBackTestRequestDto.CreateValid(Fixture);

        _mockBackTestService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<BackTestId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
