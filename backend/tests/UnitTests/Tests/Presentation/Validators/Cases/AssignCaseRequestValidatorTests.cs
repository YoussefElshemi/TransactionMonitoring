using System.Net;
using AutoFixture;
using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Models.Cases;
using Presentation.Validators.Cases;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Cases;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Users;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Cases;

namespace UnitTests.Tests.Presentation.Validators.Cases;

public class AssignCaseRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IApplicationUserService> _mockApplicationUserService;
    private readonly Mock<ICaseService> _mockCaseService;
    private readonly Mock<IOrganizationUserService> _mockOrganizationUserService;
    private readonly AssignCaseRequestValidator _sut;

    public AssignCaseRequestValidatorTests()
    {
        _mockCaseService = new Mock<ICaseService>();
        _mockOrganizationUserService = new Mock<IOrganizationUserService>();
        _mockApplicationUserService = new Mock<IApplicationUserService>();
        _sut = new AssignCaseRequestValidator(
            _mockCaseService.Object, _mockOrganizationUserService.Object, _mockApplicationUserService.Object);
    }

    [Fact]
    public async Task ValidateAsync_CaseIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeAssignCaseRequestDto.CreateValid(Fixture) with
        {
            CaseId = Guid.Empty
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CaseId)
            .WithErrorCode("NotEmptyValidator");
    }

    [Fact]
    public async Task ValidateAsync_CaseDoesNotExist_FailsValidation()
    {
        // Arrange
        var request = FakeAssignCaseRequestDto.CreateValid(Fixture);

        _mockCaseService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<CaseId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CaseId)
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithErrorMessage("Case not found");
    }

    [Fact]
    public async Task ValidateAsync_CaseStatusIsNotPending_FailsValidation()
    {
        // Arrange
        var request = FakeAssignCaseRequestDto.CreateValid(Fixture);

        _mockCaseService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<CaseId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockCaseService
            .Setup(x => x.GetAsync(It.IsAny<CaseId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeCase.CreateValid(Fixture) with { Status = CaseStatus.Assigned });

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CaseId)
            .WithErrorMessage("Case must be in a Pending status");
    }

    [Fact]
    public async Task ValidateAsync_UserIdIsEmpty_FailsValidation()
    {
        // Arrange
        var request = FakeAssignCaseRequestDto.CreateValid(Fixture);
        request = request with
        {
            Body = request.Body with
            {
                UserId = Guid.Empty
            }
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.UserId)
            .WithErrorCode("NotEqualValidator");
    }

    [Fact]
    public async Task ValidateAsync_UserIdIsNull_FailsValidation()
    {
        // Arrange
        var request = FakeAssignCaseRequestDto.CreateValid(Fixture);
        request = request with
        {
            Body = request.Body with
            {
                UserId = null
            }
        };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.UserId)
            .WithErrorCode("NotNullValidator");
    }

    [Fact]
    public async Task ValidateAsync_UserDoesNotExist_FailsValidation()
    {
        // Arrange
        var request = FakeAssignCaseRequestDto.CreateValid(Fixture);

        _mockOrganizationUserService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<ApplicationUserId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.UserId)
            .WithErrorMessage("User was not found in Organization");
    }

    [Fact]
    public async Task ValidateAsync_ValidRequest_SucceedsValidation()
    {
        // Arrange
        var request = FakeAssignCaseRequestDto.CreateValid(Fixture);

        _mockCaseService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<CaseId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockCaseService
            .Setup(x => x.GetAsync(It.IsAny<CaseId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeCase.CreateValid(Fixture) with { Status = CaseStatus.Pending });

        _mockOrganizationUserService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<ApplicationUserId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CaseId);
        result.ShouldNotHaveValidationErrorFor(x => x.Body.UserId);
    }

    [Fact]
    public async Task ValidateAsync_UserAssignsToAnotherUser_FailsValidationAsForbidden()
    {
        // Arrange
        var request = FakeAssignCaseRequestDto.CreateValid(Fixture) with
        {
            Body = new AssignCaseRequestBodyDto
            {
                UserId = Fixture.Create<Guid>()
            }
        };

        SetUpValidCaseAndUser();
        SetUpAssigningUserRoles(UserRole.User);

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveAnyValidationError()
            .WithErrorCode(HttpStatusCode.Forbidden.ToString())
            .WithErrorMessage("You cannot assign a case to another user.");
    }

    [Theory]
    [InlineData(UserRole.Manager)]
    [InlineData(UserRole.Admin)]
    public async Task ValidateAsync_ManagerOrAdminAssignsToAnotherUser_SucceedsValidation(UserRole userRole)
    {
        // Arrange
        var request = FakeAssignCaseRequestDto.CreateValid(Fixture) with
        {
            Body = new AssignCaseRequestBodyDto
            {
                UserId = Fixture.Create<Guid>()
            }
        };

        SetUpValidCaseAndUser();
        SetUpAssigningUserRoles(userRole);

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_UserAssignsToThemselves_DoesNotLookUpRoles()
    {
        // Arrange
        var request = FakeAssignCaseRequestDto.CreateValid(Fixture);

        SetUpValidCaseAndUser();

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.Errors.Should().BeEmpty();

        _mockApplicationUserService.Verify(
            x => x.GetAsync(It.IsAny<ApplicationUserId>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private void SetUpValidCaseAndUser()
    {
        _mockCaseService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<CaseId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockCaseService
            .Setup(x => x.GetAsync(It.IsAny<CaseId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeCase.CreateValid(Fixture) with { Status = CaseStatus.Pending });

        _mockOrganizationUserService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<ApplicationUserId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));
    }

    private void SetUpAssigningUserRoles(params UserRole[] userRoles)
    {
        _mockApplicationUserService
            .Setup(x => x.GetAsync(It.IsAny<ApplicationUserId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeApplicationUser.CreateValid(Fixture) with { Roles = userRoles.ToList() });
    }
}