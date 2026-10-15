using System.Net;
using Core.Interfaces.Services;
using Core.Models.Plans;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.Plans;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Plans;

namespace UnitTests.Tests.Presentation.Validators.Plans;

public class ListPlanRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IOrganizationService> _mockOrganizationService = new();
    private readonly Mock<IPlanService> _mockPlanService = new();
    private readonly ListPlanRequestValidator _sut;

    public ListPlanRequestValidatorTests()
    {
        _mockOrganizationService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockPlanService
            .Setup(x => x.GetCountAsync(It.IsAny<ListPlanRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TotalCount(100));

        _sut = new ListPlanRequestValidator(_mockOrganizationService.Object, _mockPlanService.Object, MockTimeProvider);
    }

    [Fact]
    public async Task ValidateAsync_NameExceedsMaxLength_FailsValidation()
    {
        // Arrange
        var request = FakeListPlanRequestDto.CreateValid(Fixture) with
        {
            Name = new string('a', PlanName.MaxLength + 1)
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorCode("MaximumLengthValidator");
    }

    [Fact]
    public async Task ValidateAsync_OwnerOrganizationDoesNotExist_FailsValidation()
    {
        // Arrange
        var request = FakeListPlanRequestDto.CreateValid(Fixture);

        _mockOrganizationService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.OwnerOrganizationId)
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithErrorMessage("Organization not found");
    }

    [Fact]
    public async Task ValidateAsync_OwnerOrganizationIsNull_DoesNotLookItUp()
    {
        // Arrange
        var request = FakeListPlanRequestDto.CreateValid(Fixture) with { OwnerOrganizationId = null };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();

        _mockOrganizationService.Verify(
            x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ValidateAsync_PageNumberIsBelowOne_FailsValidation(int pageNumber)
    {
        // Arrange
        var request = FakeListPlanRequestDto.CreateValid(Fixture) with { PageNumber = pageNumber };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageNumber);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(101)]
    public async Task ValidateAsync_PageSizeIsOutOfBounds_FailsValidation(int pageSize)
    {
        // Arrange
        var request = FakeListPlanRequestDto.CreateValid(Fixture) with { PageSize = pageSize };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public async Task ValidateAsync_PageNumberIsBeyondTheLastPage_FailsValidation()
    {
        // Arrange
        var request = FakeListPlanRequestDto.CreateValid(Fixture) with { PageNumber = 100, PageSize = 10 };

        _mockPlanService
            .Setup(x => x.GetCountAsync(It.IsAny<ListPlanRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TotalCount(20));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageNumber)
            .WithErrorCode("LessThanOrEqualValidator");
    }

    [Fact]
    public async Task ValidateAsync_GivenAValidRequest_SucceedsValidation()
    {
        // Arrange
        var request = FakeListPlanRequestDto.CreateValid(Fixture);

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
