using Core.Enums;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.Approvals;
using Core.Models.CaseNotes;
using Core.Models.Cases;
using Core.Models.OutboxMessages;
using Core.Services;
using Core.ValueObjects;
using FluentAssertions;
using FluentValidation;
using Moq;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Approvals;
using UnitTests.TestHelpers.FakeObjects.Core.Models.WebhookConfigurations;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.Tests.Core.Services;

public class ApprovalServiceTests : BaseTestClass
{
    private readonly Mock<IApprovalRepository> _mockApprovalRepository = new();
    private readonly Mock<IApprovalPolicyRepository> _mockApprovalPolicyRepository = new();
    private readonly Mock<ICaseService> _mockCaseService = new();
    private readonly Mock<IRuleDeploymentService> _mockRuleDeploymentService = new();
    private readonly Mock<ISuspiciousActivityReportService> _mockSuspiciousActivityReportService = new();
    private readonly Mock<ICaseNoteRepository> _mockCaseNoteRepository = new();
    private readonly Mock<IWebhookConfigurationService> _mockWebhookConfigurationService = new();
    private readonly Mock<IOutboxMessageService> _mockOutboxMessageService = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly ApprovalService _sut;

    public ApprovalServiceTests()
    {
        _sut = new ApprovalService(
            _mockApprovalRepository.Object,
            _mockApprovalPolicyRepository.Object,
            _mockCaseService.Object,
            _mockRuleDeploymentService.Object,
            _mockSuspiciousActivityReportService.Object,
            _mockCaseNoteRepository.Object,
            _mockWebhookConfigurationService.Object,
            _mockOutboxMessageService.Object,
            _mockUnitOfWork.Object,
            MockTimeProvider,
            TestModeContext);

        _mockWebhookConfigurationService
            .Setup(x => x.GetAllAsync(It.IsAny<OrganizationId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([FakeWebhookConfiguration.CreateValid(Fixture)]);
    }

    [Fact]
    public async Task ReviewAsync_RequesterApprovingTheirOwnRequest_IsRefused()
    {
        // Arrange
        var author = FakeApplicationUserId.CreateValid(Fixture);
        var approval = Pending() with { RequestedByUserId = author };

        Setup(approval);

        // Act
        var act = async () => await _sut.ReviewAsync(Review(approval, ApprovalStatus.Approved, author), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .Where(x => x.Errors.Any(error => error.ErrorCode == "Forbidden"));

        _mockApprovalRepository.Verify(x => x.ReviewAsync(It.IsAny<ReviewApprovalRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockCaseService.Verify(x => x.ApplyApprovedResolutionAsync(It.IsAny<ResolveCaseRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReviewAsync_RequesterRejectingTheirOwnRequest_IsAlsoRefused()
    {
        // Arrange
        var author = FakeApplicationUserId.CreateValid(Fixture);
        var approval = Pending() with { RequestedByUserId = author };

        Setup(approval);

        // Act
        var act = async () => await _sut.ReviewAsync(Review(approval, ApprovalStatus.Rejected, author), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .Where(x => x.Errors.Any(error => error.ErrorCode == "Forbidden"));
    }

    [Fact]
    public async Task ReviewAsync_SecondUserApproving_AppliesTheProposedResolution()
    {
        // Arrange
        var author = FakeApplicationUserId.CreateValid(Fixture);
        var reviewer = FakeApplicationUserId.CreateValid(Fixture);
        var approval = Pending() with { RequestedByUserId = author };

        Setup(approval, reviewed: approval with
        {
            Status = ApprovalStatus.Approved,
            ReviewedByUserId = reviewer,
            ReviewedAt = FakeReviewedAt.CreateValid(Fixture)
        });

        // Act
        await _sut.ReviewAsync(Review(approval, ApprovalStatus.Approved, reviewer), CancellationToken.None);

        // Assert
        _mockCaseService.Verify(x => x.ApplyApprovedResolutionAsync(
            It.Is<ResolveCaseRequest>(request =>
                request.CaseId == approval.CaseId!.Value &&
                request.Status == approval.ProposedCaseResolution!.Status &&
                request.Disposition == approval.ProposedCaseResolution.Disposition &&

                request.ResolvedByUserId == author),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReviewAsync_Rejecting_LeavesTheCaseOpenAndWritesTheReasonOntoIt()
    {
        // Arrange
        var reviewer = FakeApplicationUserId.CreateValid(Fixture);
        var approval = Pending();
        var note = FakeMessage.CreateValid(Fixture);

        Setup(approval, reviewed: approval with
        {
            Status = ApprovalStatus.Rejected,
            ReviewedByUserId = reviewer,
            ReviewedAt = FakeReviewedAt.CreateValid(Fixture),
            ReviewNote = note
        });

        // Act
        await _sut.ReviewAsync(Review(approval, ApprovalStatus.Rejected, reviewer, note), CancellationToken.None);

        // Assert
        _mockCaseService.Verify(x => x.ApplyApprovedResolutionAsync(It.IsAny<ResolveCaseRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockCaseNoteRepository.Verify(x => x.AddAsync(It.IsAny<CaseNote>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReviewAsync_ApprovingADeploymentRequest_ExecutesIt()
    {
        // Arrange
        var reviewer = FakeApplicationUserId.CreateValid(Fixture);
        var ruleDeploymentId = FakeRuleDeploymentId.CreateValid(Fixture);
        var approval = Pending() with
        {
            Subject = ApprovalSubject.RuleDeployment,
            CaseId = null,
            ProposedCaseResolution = null,
            RuleDeploymentId = ruleDeploymentId
        };

        Setup(approval, reviewed: approval with
        {
            Status = ApprovalStatus.Approved,
            ReviewedByUserId = reviewer,
            ReviewedAt = FakeReviewedAt.CreateValid(Fixture)
        });

        // Act
        await _sut.ReviewAsync(Review(approval, ApprovalStatus.Approved, reviewer), CancellationToken.None);

        // Assert
        _mockRuleDeploymentService.Verify(x => x.ExecuteApprovedAsync(ruleDeploymentId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRuleDeploymentService.Verify(x => x.CancelDeploymentAsync(It.IsAny<RuleDeploymentId>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReviewAsync_RejectingADeploymentRequest_CancelsItSoNothingIsRolledOut()
    {
        // Arrange
        var reviewer = FakeApplicationUserId.CreateValid(Fixture);
        var ruleDeploymentId = FakeRuleDeploymentId.CreateValid(Fixture);
        var approval = Pending() with
        {
            Subject = ApprovalSubject.RuleDeployment,
            CaseId = null,
            ProposedCaseResolution = null,
            RuleDeploymentId = ruleDeploymentId
        };

        Setup(approval, reviewed: approval with
        {
            Status = ApprovalStatus.Rejected,
            ReviewedByUserId = reviewer,
            ReviewedAt = FakeReviewedAt.CreateValid(Fixture),
            ReviewNote = FakeMessage.CreateValid(Fixture)
        });

        // Act
        await _sut.ReviewAsync(Review(approval, ApprovalStatus.Rejected, reviewer, FakeMessage.CreateValid(Fixture)), CancellationToken.None);

        // Assert
        _mockRuleDeploymentService.Verify(x => x.CancelDeploymentAsync(ruleDeploymentId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRuleDeploymentService.Verify(x => x.ExecuteApprovedAsync(It.IsAny<RuleDeploymentId>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ReviewAsync_SomebodyElseWithdrawing_IsRefused()
    {
        // Arrange
        var approval = Pending();

        Setup(approval);

        // Act
        var act = async () => await _sut.ReviewAsync(
            Review(approval, ApprovalStatus.Withdrawn, FakeApplicationUserId.CreateValid(Fixture)), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .Where(x => x.Errors.Any(error => error.ErrorCode == "Forbidden"));
    }

    [Fact]
    public async Task ReviewAsync_RequesterWithdrawing_SettlesTheRequestAndTouchesNothingElse()
    {
        // Arrange
        var author = FakeApplicationUserId.CreateValid(Fixture);
        var approval = Pending() with { RequestedByUserId = author };

        Setup(approval, reviewed: approval with
        {
            Status = ApprovalStatus.Withdrawn,
            ReviewedByUserId = author,
            ReviewedAt = FakeReviewedAt.CreateValid(Fixture)
        });

        // Act
        await _sut.ReviewAsync(Review(approval, ApprovalStatus.Withdrawn, author), CancellationToken.None);

        // Assert
        _mockApprovalRepository.Verify(x => x.ReviewAsync(It.IsAny<ReviewApprovalRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockCaseService.Verify(x => x.ApplyApprovedResolutionAsync(It.IsAny<ResolveCaseRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockCaseNoteRepository.Verify(x => x.AddAsync(It.IsAny<CaseNote>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(ApprovalStatus.Approved)]
    [InlineData(ApprovalStatus.Rejected)]
    [InlineData(ApprovalStatus.Withdrawn)]
    public async Task ReviewAsync_RequestThatHasAlreadySettled_IsRefused(ApprovalStatus settledAs)
    {
        // Arrange
        var approval = Pending() with { Status = settledAs };

        Setup(approval);

        // Act
        var act = async () => await _sut.ReviewAsync(
            Review(approval, ApprovalStatus.Approved, FakeApplicationUserId.CreateValid(Fixture)), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .Where(x => x.Errors.Any(error => error.ErrorCode == "Conflict"));
    }

    [Fact]
    public async Task ReviewAsync_Approving_StagesTheEventInTheSameSaveAsTheWork()
    {
        // Arrange
        var reviewer = FakeApplicationUserId.CreateValid(Fixture);
        var approval = Pending();

        Setup(approval, reviewed: approval with
        {
            Status = ApprovalStatus.Approved,
            ReviewedByUserId = reviewer,
            ReviewedAt = FakeReviewedAt.CreateValid(Fixture)
        });

        // Act
        await _sut.ReviewAsync(Review(approval, ApprovalStatus.Approved, reviewer), CancellationToken.None);

        // Assert
        _mockOutboxMessageService.Verify(x => x.StageManyAsync(It.IsAny<List<CreateOutboxMessageRequest>>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockOutboxMessageService.Verify(x => x.CreateManyAsync(It.IsAny<List<CreateOutboxMessageRequest>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private Approval Pending()
    {
        return FakeApproval.CreateValid(Fixture);
    }

    private void Setup(Approval approval, Approval? reviewed = null)
    {
        _mockApprovalRepository
            .Setup(x => x.GetAsync(It.IsAny<ApprovalId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(approval);

        _mockApprovalRepository
            .Setup(x => x.ReviewAsync(It.IsAny<ReviewApprovalRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviewed ?? approval);
    }

    private static ReviewApprovalRequest Review(
        Approval approval, ApprovalStatus status, ApplicationUserId reviewedByUserId, Message? note = null)
    {
        return new ReviewApprovalRequest
        {
            ApprovalId = approval.ApprovalId,
            Status = status,
            ReviewNote = note,
            ReviewedByUserId = reviewedByUserId
        };
    }
}
