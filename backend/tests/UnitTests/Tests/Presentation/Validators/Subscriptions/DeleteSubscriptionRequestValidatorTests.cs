using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.Subscriptions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Subscriptions;

namespace UnitTests.Tests.Presentation.Validators.Subscriptions;

public class DeleteSubscriptionRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IOrganizationService> _mockOrganizationService = new();
    private readonly Mock<ISubscriptionService> _mockSubscriptionService = new();
    private readonly DeleteSubscriptionRequestValidator _sut;

    public DeleteSubscriptionRequestValidatorTests()
    {
        _mockOrganizationService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockSubscriptionService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<SubscriptionId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _sut = new DeleteSubscriptionRequestValidator(_mockOrganizationService.Object, _mockSubscriptionService.Object);
    }

    [Fact]
    public async Task ValidateAsync_OrganizationDoesNotExist_FailsValidation()
    {
        // Arrange
        var request = FakeDeleteSubscriptionRequestDto.CreateValid(Fixture);

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
    public async Task ValidateAsync_SubscriptionIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeDeleteSubscriptionRequestDto.CreateValid(Fixture) with { SubscriptionId = Guid.Empty };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SubscriptionId)
            .WithErrorCode("NotEmptyValidator");
    }

    [Fact]
    public async Task ValidateAsync_SubscriptionDoesNotExist_FailsValidation()
    {
        // Arrange
        var request = FakeDeleteSubscriptionRequestDto.CreateValid(Fixture);

        _mockSubscriptionService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<SubscriptionId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SubscriptionId)
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithErrorMessage("Subscription not found");
    }

    [Fact]
    public async Task ValidateAsync_GivenASubscriptionId_ChecksItAgainstTheOrganizationOnTheRoute()
    {
        // Arrange
        var request = FakeDeleteSubscriptionRequestDto.CreateValid(Fixture);

        // Act
        await _sut.TestValidateAsync(request);

        // Assert
        _mockSubscriptionService.Verify(
            x => x.ExistsAsync(
                new OrganizationId(request.OrganizationId),
                new SubscriptionId(request.SubscriptionId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateAsync_GivenAValidRequest_SucceedsValidation()
    {
        // Arrange
        var request = FakeDeleteSubscriptionRequestDto.CreateValid(Fixture);

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
