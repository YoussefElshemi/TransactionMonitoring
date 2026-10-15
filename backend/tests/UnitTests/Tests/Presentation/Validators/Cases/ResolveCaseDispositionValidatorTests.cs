using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation.TestHelper;
using Moq;
using Presentation.Models.Cases;
using Presentation.Validators.Cases;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.CaseReasonCodes;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Cases;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Cases;

namespace UnitTests.Tests.Presentation.Validators.Cases;

public class ResolveCaseDispositionValidatorTests : BaseTestClass
{
    private readonly Mock<ICaseService> _mockCaseService = new();
    private readonly Mock<ICaseReasonCodeService> _mockCaseReasonCodeService = new();
    private readonly Mock<IOrganizationUserService> _mockOrganizationUserService = new();
    private readonly ResolveCaseRequestValidator _sut;

    public ResolveCaseDispositionValidatorTests()
    {
        _mockCaseService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<CaseId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockOrganizationUserService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<ApplicationUserId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockCaseReasonCodeService
            .Setup(x => x.AnyEnabledAsync(It.IsAny<OrganizationId>(), It.IsAny<CaseDisposition?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(false));

        _sut = new ResolveCaseRequestValidator(_mockCaseService.Object, _mockCaseReasonCodeService.Object, _mockOrganizationUserService.Object);
    }

    [Fact]
    public async Task ValidateAsync_MissingDisposition_FailsValidation()
    {
        // Arrange
        var request = BuildRequest(CaseStatus.Approved, disposition: string.Empty, note: null);

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.Disposition);
    }

    [Fact]
    public async Task ValidateAsync_UnknownDisposition_FailsValidation()
    {
        // Arrange
        var request = BuildRequest(CaseStatus.Approved, disposition: "Maybe", note: null);

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.Disposition);
    }

    [Fact]
    public async Task ValidateAsync_RejectionWithoutANote_FailsValidation()
    {
        // Arrange
        var request = BuildRequest(CaseStatus.Rejected, nameof(CaseDisposition.FalsePositive), note: null);

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.Note);
    }

    [Fact]
    public async Task ValidateAsync_TruePositiveWithoutANote_FailsValidation()
    {
        // Arrange
        var request = BuildRequest(CaseStatus.Approved, nameof(CaseDisposition.TruePositive), note: null);

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.Note);
    }

    [Fact]
    public async Task ValidateAsync_FalseNegativeWithoutANote_FailsValidation()
    {
        // Arrange
        var request = BuildRequest(CaseStatus.Approved, nameof(CaseDisposition.FalseNegative), note: null);

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.Note);
    }

    [Theory]
    [InlineData(nameof(CaseDisposition.FalsePositive))]
    [InlineData(nameof(CaseDisposition.TrueNegative))]
    public async Task ValidateAsync_ApprovedCloseThatFoundNothingWithoutANote_SucceedsValidation(string disposition)
    {
        // Arrange
        var request = BuildRequest(CaseStatus.Approved, disposition, note: null);

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Body.Note);
    }

    [Theory]
    [InlineData(nameof(CaseDisposition.TruePositive))]
    [InlineData(nameof(CaseDisposition.FalsePositive))]
    [InlineData(nameof(CaseDisposition.TrueNegative))]
    [InlineData(nameof(CaseDisposition.FalseNegative))]
    public async Task ValidateAsync_AnyDeclaredDisposition_IsAccepted(string disposition)
    {
        // Arrange
        var request = BuildRequest(CaseStatus.Approved, disposition, note: "Reviewed by the desk");

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Body.Disposition);
    }

    [Fact]
    public async Task ValidateAsync_NoReasonCodeWhereTheOrganizationHasDeclaredNone_SucceedsValidation()
    {
        // Arrange
        var request = BuildRequest(CaseStatus.Approved, nameof(CaseDisposition.FalsePositive), note: null);

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Body.ReasonCodeId);
    }

    [Fact]
    public async Task ValidateAsync_NoReasonCodeWhereTheOrganizationHasDeclaredOne_FailsValidation()
    {
        // Arrange
        _mockCaseReasonCodeService
            .Setup(x => x.AnyEnabledAsync(It.IsAny<OrganizationId>(), It.IsAny<CaseDisposition?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        var request = BuildRequest(CaseStatus.Approved, nameof(CaseDisposition.FalsePositive), note: null);

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.ReasonCodeId);
    }

    [Fact]
    public async Task ValidateAsync_ReasonCodeBoundToTheOtherDisposition_FailsValidation()
    {
        // Arrange
        SetupReasonCode(CaseDisposition.TruePositive, isEnabled: true);

        var request = BuildRequest(CaseStatus.Approved, nameof(CaseDisposition.FalsePositive), note: null, reasonCodeId: Guid.NewGuid());

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.ReasonCodeId);
    }

    [Fact]
    public async Task ValidateAsync_RetiredReasonCode_FailsValidation()
    {
        // Arrange
        SetupReasonCode(CaseDisposition.FalsePositive, isEnabled: false);

        var request = BuildRequest(CaseStatus.Approved, nameof(CaseDisposition.FalsePositive), note: null, reasonCodeId: Guid.NewGuid());

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Body.ReasonCodeId);
    }

    [Fact]
    public async Task ValidateAsync_EnabledReasonCodeForThisDisposition_SucceedsValidation()
    {
        // Arrange
        SetupReasonCode(CaseDisposition.FalsePositive, isEnabled: true);

        var request = BuildRequest(CaseStatus.Approved, nameof(CaseDisposition.FalsePositive), note: null, reasonCodeId: Guid.NewGuid());

        // Act
        var result = await _sut.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Body.ReasonCodeId);
    }

    private void SetupReasonCode(CaseDisposition disposition, bool isEnabled)
    {
        _mockCaseReasonCodeService
            .Setup(x => x.ExistsAsync(It.IsAny<OrganizationId>(), It.IsAny<CaseReasonCodeId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exists(true));

        _mockCaseReasonCodeService
            .Setup(x => x.GetAsync(It.IsAny<CaseReasonCodeId>(), It.IsAny<IncludeDeleted>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeCaseReasonCode.CreateValid(Fixture) with
            {
                Disposition = disposition,
                IsEnabled = new IsEnabled(isEnabled)
            });
    }

    private ResolveCaseRequestDto BuildRequest(CaseStatus status, string disposition, string? note, Guid? reasonCodeId = null)
    {
        var request = FakeResolveCaseRequestDto.CreateValid(Fixture);

        _mockCaseService
            .Setup(x => x.GetAsync(It.IsAny<CaseId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeCase.CreateValid(Fixture) with
            {
                Status = CaseStatus.Assigned,
                AssignedToUserId = new ApplicationUserId(request.Body.UserId)
            });

        return request with
        {
            Body = new ResolveCaseRequestBodyDto
            {
                Status = status.ToString(),
                Disposition = disposition,
                ReasonCodeId = reasonCodeId,
                Note = note,
                UserId = request.Body.UserId
            }
        };
    }
}
