using System.Net;
using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.Cases;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Cases;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Cases;

namespace UnitTests.Tests.Presentation.Validators.Cases;

public class ResolveCaseRequestValidatorTests : BaseTestClass
{
    private readonly Mock<ICaseService> _mockCaseService;
    private readonly Mock<ICaseReasonCodeService> _mockCaseReasonCodeService;
    private readonly Mock<IOrganizationUserService> _mockOrganizationUserService;
    private readonly ResolveCaseRequestValidator _sut;

    public ResolveCaseRequestValidatorTests()
    {
        _mockCaseService = new Mock<ICaseService>();
        _mockCaseReasonCodeService = new Mock<ICaseReasonCodeService>();
        _mockOrganizationUserService = new Mock<IOrganizationUserService>();
        _sut = new ResolveCaseRequestValidator(_mockCaseService.Object, _mockCaseReasonCodeService.Object, _mockOrganizationUserService.Object);
    }

    [Fact]
    public async Task ValidateAsync_CaseIdNotEmpty_SucceedsValidation()
    {
        // Arrange
        var request = FakeResolveCaseRequestDto.CreateValid(Fixture);

        _mockCaseService.Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<CaseId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockCaseService
            .Setup(x => x.GetAsync(It.IsAny<CaseId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeCase.CreateValid(Fixture) with
            {
                Status = CaseStatus.Assigned
            });

        _mockOrganizationUserService.Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<ApplicationUserId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CaseId);
    }

    [Fact]
    public async Task ValidateAsync_CaseNotFound_FailsValidation()
    {
        // Arrange
        var request = FakeResolveCaseRequestDto.CreateValid(Fixture);

        _mockCaseService.Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<CaseId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CaseId)
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithErrorMessage("Case not found");
    }

    [Fact]
    public async Task ValidateAsync_UserDoesNotExist_FailsValidation()
    {
        // Arrange
        var request = FakeResolveCaseRequestDto.CreateValid(Fixture);

        _mockCaseService.Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<CaseId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockCaseService
            .Setup(x => x.GetAsync(It.IsAny<CaseId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeCase.CreateValid(Fixture) with
            {
                Status = CaseStatus.Assigned
            });

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
    public async Task ValidateAsync_AssignedToUserIdMismatch_FailsValidation()
    {
        // Arrange
        var request = FakeResolveCaseRequestDto.CreateValid(Fixture);

        _mockCaseService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<CaseId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockCaseService
            .Setup(x => x.GetAsync(It.IsAny<CaseId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeCase.CreateValid(Fixture) with
            {
                Status = CaseStatus.Assigned
            });

        _mockOrganizationUserService
            .SetupSequence(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<ApplicationUserId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.UserId)
            .WithErrorMessage("Case must be assigned to user attempting to resolve");
    }

    [Fact]
    public async Task ValidateAsync_ValidStatus_SucceedsValidation()
    {
        // Arrange
        var request = FakeResolveCaseRequestDto.CreateValid(Fixture);
        request = request with
        {
            Body = request.Body with
            {
                Status = "Approved"
            }
        };

        _mockCaseService.Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<CaseId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockCaseService
            .Setup(x => x.GetAsync(It.IsAny<CaseId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeCase.CreateValid(Fixture) with
            {
                Status = CaseStatus.Assigned
            });

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Body.Status);
    }

    [Fact]
    public async Task ValidateAsync_InvalidStatus_FailsValidation()
    {
        // Arrange
        var request = FakeResolveCaseRequestDto.CreateValid(Fixture);
        request = request with
        {
            Body = request.Body with
            {
                Status = CaseStatus.Pending.ToString()
            }
        };

        _mockCaseService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<CaseId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockCaseService
            .Setup(x => x.GetAsync(It.IsAny<CaseId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeCase.CreateValid(Fixture) with
            {
                Status = CaseStatus.Assigned
            });

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.Status)
            .WithErrorMessage("Status must be one of 'Approved', 'Rejected'");
    }

    [Theory]
    [InlineData(CaseStatus.Approved)]
    [InlineData(CaseStatus.Rejected)]
    [InlineData(CaseStatus.Expired)]
    public async Task ValidateAsync_CaseAlreadyResolved_FailsValidation(CaseStatus caseStatus)
    {
        // Arrange
        var request = FakeResolveCaseRequestDto.CreateValid(Fixture);

        _mockCaseService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<CaseId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockCaseService
            .Setup(x => x.GetAsync(It.IsAny<CaseId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeCase.CreateValid(Fixture) with
            {
                Status = caseStatus
            });

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CaseId)
            .WithErrorMessage("Case has already been resolved");
    }
}