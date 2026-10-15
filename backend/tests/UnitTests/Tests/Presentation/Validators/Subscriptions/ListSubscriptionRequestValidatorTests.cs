using System.Net;
using Core.Interfaces.Services;
using Core.Models.Subscriptions;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.Subscriptions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Subscriptions;

namespace UnitTests.Tests.Presentation.Validators.Subscriptions;

public class ListSubscriptionRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IOrganizationService> _mockOrganizationService = new();
    private readonly Mock<ISubscriptionService> _mockSubscriptionService = new();
    private readonly ListSubscriptionRequestValidator _sut;

    public ListSubscriptionRequestValidatorTests()
    {
        _mockOrganizationService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockSubscriptionService
            .Setup(x => x.GetCountAsync(It.IsAny<ListSubscriptionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TotalCount(100));

        _sut = new ListSubscriptionRequestValidator(
            _mockOrganizationService.Object, _mockSubscriptionService.Object, MockTimeProvider);
    }

    [Fact]
    public async Task ValidateAsync_OrganizationIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeListSubscriptionRequestDto.CreateValid(Fixture) with { OrganizationId = Guid.Empty };

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
        var request = FakeListSubscriptionRequestDto.CreateValid(Fixture);

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

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ValidateAsync_PageNumberIsBelowOne_FailsValidation(int pageNumber)
    {
        // Arrange
        var request = FakeListSubscriptionRequestDto.CreateValid(Fixture) with { PageNumber = pageNumber };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageNumber);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task ValidateAsync_PageSizeIsOutOfBounds_FailsValidation(int pageSize)
    {
        // Arrange
        var request = FakeListSubscriptionRequestDto.CreateValid(Fixture) with { PageSize = pageSize };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public async Task ValidateAsync_GivenAValidRequest_SucceedsValidation()
    {
        // Arrange
        var request = FakeListSubscriptionRequestDto.CreateValid(Fixture);

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
