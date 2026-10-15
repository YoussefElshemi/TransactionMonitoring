using System.Net;
using Core.Interfaces.Services;
using Core.Models.Organizations;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.OrganizationUsers;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.OrganizationUsers;

namespace UnitTests.Tests.Presentation.Validators.OrganizationUsers;

public class ListOrganizationUserRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IOrganizationService> _mockOrganizationService;
    private readonly Mock<IOrganizationUserService> _mockOrganizationUserService;
    private readonly ListOrganizationUserRequestValidator _sut;

    public ListOrganizationUserRequestValidatorTests()
    {
        _mockOrganizationUserService = new Mock<IOrganizationUserService>();
        _mockOrganizationService = new Mock<IOrganizationService>();
        _sut = new ListOrganizationUserRequestValidator(_mockOrganizationUserService.Object, _mockOrganizationService.Object, MockTimeProvider);
    }

    [Fact]
    public async Task ValidateAsync_OrganizationIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeListOrganizationUserRequestDto.CreateValid(Fixture) with
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
        var request = FakeListOrganizationUserRequestDto.CreateValid(Fixture);

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
    public async Task ValidateAsync_PageNumberExceedsMaxPageCount_FailsValidation()
    {
        // Arrange
        var request = FakeListOrganizationUserRequestDto.CreateValid(Fixture) with
        {
            PageSize = 1,
            PageNumber = 2
        };

        _mockOrganizationService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockOrganizationUserService
            .Setup(x => x.GetCountAsync(It.IsAny<ListOrganizationUserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TotalCount(1));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageNumber)
            .WithErrorCode("LessThanOrEqualValidator");
    }

    [Fact]
    public async Task ValidateAsync_PageSizeExceedsMax_FailsValidation()
    {
        // Arrange
        var request = FakeListOrganizationUserRequestDto.CreateValid(Fixture) with
        {
            PageSize = 200
        };

        _mockOrganizationService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageSize)
            .WithErrorCode("LessThanOrEqualValidator");
    }

    [Fact]
    public async Task ValidateAsync_FromDateInFuture_FailsValidation()
    {
        // Arrange
        var request = FakeListOrganizationUserRequestDto.CreateValid(Fixture) with
        {
            FromDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime.AddDays(1))
        };

        _mockOrganizationService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FromDate)
            .WithErrorCode("LessThanOrEqualValidator");
    }

    [Fact]
    public async Task ValidateAsync_FromDateAfterToDate_FailsValidation()
    {
        // Arrange
        var request = FakeListOrganizationUserRequestDto.CreateValid(Fixture) with
        {
            FromDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime.AddDays(1))
        };

        _mockOrganizationService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FromDate)
            .WithErrorCode("LessThanOrEqualValidator");
    }

    [Fact]
    public async Task ValidateAsync_ValidRequest_SucceedsValidation()
    {
        // Arrange
        var request = FakeListOrganizationUserRequestDto.CreateValid(Fixture) with
        {
            PageNumber = 1,
            PageSize = 10
        };

        _mockOrganizationService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockOrganizationUserService
            .Setup(x => x.GetCountAsync(It.IsAny<ListOrganizationUserRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TotalCount(100));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PageNumber);
        result.ShouldNotHaveValidationErrorFor(x => x.PageSize);
        result.ShouldNotHaveValidationErrorFor(x => x.FromDate);
        result.ShouldNotHaveValidationErrorFor(x => x.ToDate);
    }
}