using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.Cases;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.Cases;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Cases;

namespace UnitTests.Tests.Presentation.Validators.Cases;

public class ListCaseRequestValidatorTests : BaseTestClass
{
    private readonly Mock<ICaseService> _mockCaseService;
    private readonly Mock<IOrganizationUserService> _mockOrganizationUserService;
    private readonly ListCaseRequestValidator _sut;

    public ListCaseRequestValidatorTests()
    {
        _mockCaseService = new Mock<ICaseService>();
        _mockOrganizationUserService = new Mock<IOrganizationUserService>();
        _sut = new ListCaseRequestValidator(_mockCaseService.Object, _mockOrganizationUserService.Object, MockTimeProvider);
    }

    [Fact]
    public async Task ValidateAsync_InvalidStatus_FailsValidation()
    {
        // Arrange
        var request = FakeListCaseRequestDto.CreateValid(Fixture) with
        {
            Status = "Invalid"
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Status)
            .WithErrorCode("StringEnumValidator");
    }

    [Fact]
    public async Task ValidateAsync_ValidStatus_SucceedsValidation()
    {
        // Arrange
        var request = FakeListCaseRequestDto.CreateValid(Fixture) with
        {
            Status = CaseStatus.Pending.ToString()
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public async Task ValidateAsync_InvalidSeverity_FailsValidation()
    {
        // Arrange
        var request = FakeListCaseRequestDto.CreateValid(Fixture) with
        {
            Severity = "Invalid"
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Severity)
            .WithErrorCode("StringEnumValidator");
    }

    [Fact]
    public async Task ValidateAsync_ValidSeverity_SucceedsValidation()
    {
        // Arrange
        var request = FakeListCaseRequestDto.CreateValid(Fixture) with
        {
            Severity = RuleSeverity.Critical.ToString()
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Severity);
    }

    [Fact]
    public async Task ValidateAsync_AssignedToUserIdDoesNotExist_FailsValidation()
    {
        // Arrange
        var request = FakeListCaseRequestDto.CreateValid(Fixture);

        _mockOrganizationUserService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<ApplicationUserId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AssignedToUserId)
            .WithErrorMessage("User was not found in Organization");
    }

    [Fact]
    public async Task ValidateAsync_AssignedToUserIdExists_SucceedsValidation()
    {
        // Arrange
        var request = FakeListCaseRequestDto.CreateValid(Fixture);

        _mockOrganizationUserService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<ApplicationUserId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.AssignedToUserId);
    }

    [Fact]
    public async Task ValidateAsync_InvalidPageNumber_FailsValidation()
    {
        // Arrange
        var request = FakeListCaseRequestDto.CreateValid(Fixture) with
        {
            PageNumber = 0
        };

        _mockOrganizationUserService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<ApplicationUserId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageNumber)
            .WithErrorCode("GreaterThanOrEqualValidator");
    }

    [Fact]
    public async Task ValidateAsync_InvalidPageSize_FailsValidation()
    {
        // Arrange
        var request = FakeListCaseRequestDto.CreateValid(Fixture) with
        {
            PageSize = 0
        };

        _mockOrganizationUserService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<ApplicationUserId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageSize)
            .WithErrorCode("GreaterThanOrEqualValidator");
    }

    [Fact]
    public async Task ValidateAsync_ExceedsMaxPageSize_FailsValidation()
    {
        // Arrange
        var request = FakeListCaseRequestDto.CreateValid(Fixture) with
        {
            PageSize = 101
        };

        _mockOrganizationUserService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<ApplicationUserId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageSize)
            .WithErrorCode("LessThanOrEqualValidator");
    }

    [Fact]
    public async Task ValidateAsync_ValidPagination_SucceedsValidation()
    {
        // Arrange
        var request = FakeListCaseRequestDto.CreateValid(Fixture) with
        {
            PageNumber = 1,
            PageSize = 50
        };

        _mockCaseService
            .Setup(x => x.GetCountAsync(It.IsAny<ListCaseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TotalCount(100));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PageNumber);
        result.ShouldNotHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public async Task ValidateAsync_InvalidDateRange_FailsValidation()
    {
        // Arrange
        var request = FakeListCaseRequestDto.CreateValid(Fixture) with
        {
            FromDate = DateOnly.FromDateTime(MockTimeProvider.GetUtcNow().UtcDateTime.AddDays(1))
        };

        _mockOrganizationUserService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<ApplicationUserId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FromDate)
            .WithErrorCode("LessThanOrEqualValidator");
    }

    [Fact]
    public async Task ValidateAsync_ValidDateRange_SucceedsValidation()
    {
        // Arrange
        var request = FakeListCaseRequestDto.CreateValid(Fixture);

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FromDate);
        result.ShouldNotHaveValidationErrorFor(x => x.ToDate);
    }

    [Fact]
    public async Task ValidateAsync_PageNumberGreaterThanMax_FailsValidation()
    {
        // Arrange
        var request = FakeListCaseRequestDto.CreateValid(Fixture) with
        {
            PageNumber = 999,
            PageSize = 10
        };

        _mockOrganizationUserService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<ApplicationUserId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockCaseService
            .Setup(x => x.GetCountAsync(It.IsAny<ListCaseRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TotalCount(100));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PageNumber)
            .WithErrorCode("LessThanOrEqualValidator");
    }
}