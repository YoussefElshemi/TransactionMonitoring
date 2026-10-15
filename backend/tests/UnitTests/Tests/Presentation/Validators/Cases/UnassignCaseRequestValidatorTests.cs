using System.Net;
using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.Cases;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.Cases;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Cases;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Cases;

namespace UnitTests.Tests.Presentation.Validators.Cases;

public class UnassignCaseRequestValidatorTests : BaseTestClass
{
    private readonly Mock<ICaseService> _mockCaseService;
    private readonly UnassignCaseRequestValidator _sut;

    public UnassignCaseRequestValidatorTests()
    {
        _mockCaseService = new Mock<ICaseService>();
        _sut = new UnassignCaseRequestValidator(_mockCaseService.Object);
    }

    [Fact]
    public async Task ValidateAsync_CaseIdNotEmpty_SucceedsValidation()
    {
        // Arrange
        var request = FakeUnassignCaseRequestDto.CreateValid(Fixture);

        _mockCaseService.Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<CaseId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockCaseService.Setup(x => x.GetAsync(It.IsAny<CaseId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeCase.CreateValid(Fixture) with { Status = CaseStatus.Assigned });

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CaseId);
    }

    [Fact]
    public async Task ValidateAsync_CaseNotFound_FailsValidation()
    {
        // Arrange
        var request = FakeUnassignCaseRequestDto.CreateValid(Fixture);
        var caseModel = FakeCase.CreateValid(Fixture) with
        {
            Status = CaseStatus.Assigned
        };

        _mockCaseService.Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<CaseId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        _mockCaseService.Setup(x => x.GetAsync(It.IsAny<CaseId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(caseModel);

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CaseId)
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithErrorMessage("Case not found");
    }

    [Fact]
    public async Task ValidateAsync_CaseNotAssigned_FailsValidation()
    {
        // Arrange
        var request = FakeUnassignCaseRequestDto.CreateValid(Fixture);
        var caseModel = FakeCase.CreateValid(Fixture) with
        {
            Status = CaseStatus.Pending,
            AssignedToUserId = null,
            AssignedToUser = null
        };

        _mockCaseService.Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<CaseId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockCaseService.Setup(x => x.GetAsync(It.IsAny<CaseId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(caseModel);

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CaseId)
            .WithErrorMessage($"{nameof(Case)} must be assigned and still open");
    }

    [Fact]
    public async Task ValidateAsync_CaseAlreadyResolved_FailsValidation()
    {
        // Arrange
        var request = FakeUnassignCaseRequestDto.CreateValid(Fixture);
        var caseModel = FakeCase.CreateValid(Fixture) with
        {
            Status = CaseStatus.Approved
        };

        _mockCaseService.Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<CaseId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockCaseService.Setup(x => x.GetAsync(It.IsAny<CaseId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(caseModel);

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CaseId)
            .WithErrorMessage($"{nameof(Case)} must be assigned and still open");
    }

    [Theory]
    [InlineData(CaseStatus.InReview)]
    [InlineData(CaseStatus.Escalated)]
    [InlineData(CaseStatus.PendingInformation)]
    [InlineData(CaseStatus.OnHold)]
    public async Task ValidateAsync_CaseInADeskState_SucceedsValidation(CaseStatus caseStatus)
    {
        // Arrange
        var request = FakeUnassignCaseRequestDto.CreateValid(Fixture);
        var caseModel = FakeCase.CreateValid(Fixture) with
        {
            Status = caseStatus
        };

        _mockCaseService.Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<CaseId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockCaseService.Setup(x => x.GetAsync(It.IsAny<CaseId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(caseModel);

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CaseId);
    }

    [Fact]
    public async Task ValidateAsync_CaseAssigned_SucceedsValidation()
    {
        // Arrange
        var request = FakeUnassignCaseRequestDto.CreateValid(Fixture);
        var caseModel = FakeCase.CreateValid(Fixture) with
        {
            Status = CaseStatus.Assigned // Simulate case in "Assigned" status
        };

        _mockCaseService.Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<CaseId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockCaseService.Setup(x => x.GetAsync(It.IsAny<CaseId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(caseModel);

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CaseId);
    }
}