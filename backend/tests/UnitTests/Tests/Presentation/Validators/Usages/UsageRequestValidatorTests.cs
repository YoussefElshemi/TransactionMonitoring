using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.Usages;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Usages;

namespace UnitTests.Tests.Presentation.Validators.Usages;

public class UsageRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IUsageService> _mockUsageService;
    private readonly UsageRequestValidator _sut;

    public UsageRequestValidatorTests()
    {
        _mockUsageService = new Mock<IUsageService>();
        _sut = new UsageRequestValidator(_mockUsageService.Object);
    }

    [Fact]
    public async Task ValidateAsync_UsageIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeUsageRequestDto.CreateValid(Fixture) with
        {
            UsageId = Guid.Empty
        };


        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UsageId)
            .WithErrorCode("NotEmptyValidator");
    }

    [Fact]
    public async Task ValidateAsync_UsageIdDoesNotExist_FailsValidation()
    {
        // Arrange
        var request = FakeUsageRequestDto.CreateValid(Fixture);

        _mockUsageService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<UsageId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UsageId)
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithErrorMessage("Usage not found");
    }

    [Fact]
    public async Task ValidateAsync_UsageIdDoesExist_SucceedsValidation()
    {
        // Arrange
        var request = FakeUsageRequestDto.CreateValid(Fixture);

        _mockUsageService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<UsageId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.UsageId);
    }
}