using System.Net;
using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Validators.Decisions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Decisions;

namespace UnitTests.Tests.Presentation.Validators.Decisions;

public class ResolveDecisionRequestValidatorTests : BaseTestClass
{
    private readonly Mock<IDecisionService> _mockDecisionService = new();
    private readonly Mock<ICaseReasonCodeService> _mockCaseReasonCodeService = new();
    private readonly Mock<IOrganizationUserService> _mockOrganizationUserService = new();
    private readonly ResolveDecisionRequestValidator _sut;

    public ResolveDecisionRequestValidatorTests()
    {
        _mockDecisionService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<DecisionId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockOrganizationUserService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<ApplicationUserId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _sut = new ResolveDecisionRequestValidator(_mockDecisionService.Object, _mockCaseReasonCodeService.Object, _mockOrganizationUserService.Object);
    }

    [Fact]
    public async Task ValidateAsync_KnownDecision_SucceedsValidation()
    {
        // Arrange
        var request = FakeResolveDecisionRequestDto.CreateValid(Fixture);

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task ValidateAsync_UnknownDecision_FailsWithNotFound()
    {
        // Arrange
        var request = FakeResolveDecisionRequestDto.CreateValid(Fixture);

        _mockDecisionService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<DecisionId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DecisionId)
            .WithErrorCode(nameof(HttpStatusCode.NotFound))
            .WithErrorMessage("Decision not found");
    }

    [Fact]
    public async Task ValidateAsync_UserOutsideTheOrganization_FailsValidation()
    {
        // Arrange
        var request = FakeResolveDecisionRequestDto.CreateValid(Fixture);

        _mockOrganizationUserService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<ApplicationUserId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.UserId);
    }

    [Theory]
    [InlineData(nameof(CaseStatus.Approved))]
    [InlineData(nameof(CaseStatus.Rejected))]
    [InlineData("approved")]
    [InlineData("REJECTED")]
    public async Task ValidateAsync_UsableVerdict_SucceedsValidation(string status)
    {
        // Arrange
        var request = FakeResolveDecisionRequestDto.CreateValid(Fixture);
        request = request with { Body = request.Body with { Status = status } };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Body.Status);
    }

    [Theory]
    [InlineData(nameof(CaseStatus.Pending))]
    [InlineData(nameof(CaseStatus.Assigned))]
    [InlineData(nameof(CaseStatus.Expired))]
    public async Task ValidateAsync_UnreachableVerdict_FailsValidation(string status)
    {
        // Arrange
        var request = FakeResolveDecisionRequestDto.CreateValid(Fixture);
        request = request with { Body = request.Body with { Status = status } };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.Status)
            .WithErrorMessage($"Status must be one of '{nameof(CaseStatus.Approved)}', '{nameof(CaseStatus.Rejected)}'");
    }

    [Fact]
    public async Task ValidateAsync_StatusThatIsNotACaseStatus_FailsValidation()
    {
        // Arrange
        var request = FakeResolveDecisionRequestDto.CreateValid(Fixture);
        request = request with { Body = request.Body with { Status = "Released" } };

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.Status)
            .WithErrorCode("StringEnumValidator");
    }

    [Fact]
    public async Task ValidateAsync_ProbesTheDecisionWithinTheCallersOrganization()
    {
        // Arrange
        var request = FakeResolveDecisionRequestDto.CreateValid(Fixture);

        // Act
        await _sut.TestValidateAsync(request);

        // Assert
        _mockDecisionService.Verify(
            x => x.ExistsAsync(
                new OrganizationId(request.OrganizationId),
                new DecisionId(request.DecisionId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
