using Core.Constants;
using Core.Enums;
using Core.Exceptions;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.Users;
using Core.Services;
using Core.ValueObjects;
using FluentAssertions;
using Moq;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Users;
using UnitTests.TestHelpers.FakeObjects.Core.ValueObjects;

namespace UnitTests.Tests.Core.Services;

public class OffboardingServiceTests : BaseTestClass
{
    private readonly Mock<IApplicationUserService> _mockApplicationUserService = new();
    private readonly Mock<ICaseRepository> _mockCaseRepository = new();
    private readonly Mock<IQueueMemberRepository> _mockQueueMemberRepository = new();
    private readonly Mock<IRoleAssignmentRepository> _mockRoleAssignmentRepository = new();
    private readonly Mock<IInvitationRepository> _mockInvitationRepository = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly OffboardingService _sut;

    private readonly OrganizationId _organizationId;

    public OffboardingServiceTests()
    {
        _organizationId = FakeOrganizationId.CreateValid(Fixture);

        _sut = new OffboardingService(
            _mockApplicationUserService.Object,
            _mockCaseRepository.Object,
            _mockQueueMemberRepository.Object,
            _mockRoleAssignmentRepository.Object,
            _mockInvitationRepository.Object,
            _mockUnitOfWork.Object);
    }

    [Fact]
    public async Task OffboardAsync_ASuccessorIsNamed_MovesTheOpenWorkAndTheQueuesToThem()
    {
        // Arrange
        var request = CreateRequest(FakeApplicationUserId.CreateValid(Fixture));
        GivenUser(request.UserId, _organizationId);
        GivenUser(request.SuccessorUserId!.Value, _organizationId);

        _mockCaseRepository
            .Setup(x => x.ReassignOpenCasesAsync(_organizationId, request.UserId, request.SuccessorUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Count(12));

        _mockQueueMemberRepository
            .Setup(x => x.ReplaceMemberAsync(_organizationId, request.UserId, request.SuccessorUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Count(4));

        // Act
        var offboarded = await _sut.OffboardAsync(request, CancellationToken.None);

        // Assert
        ((int)offboarded.ReassignedCaseCount).Should().Be(12);
        ((int)offboarded.QueueMembershipCount).Should().Be(4);
        offboarded.SuccessorUserId.Should().Be(request.SuccessorUserId);
    }

    [Fact]
    public async Task OffboardAsync_NobodyIsNamed_HandsTheOpenWorkBackToThePool()
    {
        // Arrange
        var request = CreateRequest(null);
        GivenUser(request.UserId, _organizationId);

        // Act
        await _sut.OffboardAsync(request, CancellationToken.None);

        // Assert
        _mockCaseRepository.Verify(
            x => x.ReassignOpenCasesAsync(_organizationId, request.UserId, null, It.IsAny<CancellationToken>()), Times.Once);
        _mockQueueMemberRepository.Verify(
            x => x.ReplaceMemberAsync(_organizationId, request.UserId, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OffboardAsync_Always_ReleasesTheAccountOnlyAfterTheWorkHasMoved()
    {
        // Arrange
        var request = CreateRequest(null);
        var user = GivenUser(request.UserId, _organizationId);

        var order = new List<string>();

        _mockCaseRepository
            .Setup(x => x.ReassignOpenCasesAsync(It.IsAny<OrganizationId>(), It.IsAny<ApplicationUserId>(), It.IsAny<ApplicationUserId?>(), It.IsAny<CancellationToken>()))
            .Callback(() => order.Add("cases"))
            .ReturnsAsync(new Count(0));

        _mockApplicationUserService
            .Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
            .Callback(() => order.Add("release"));

        // Act
        await _sut.OffboardAsync(request, CancellationToken.None);

        // Assert
        order.Should().Equal("cases", "release");
        user.OrganizationId.Should().Be(new OrganizationId(SystemConstants.DefaultOrganization));
    }

    [Fact]
    public async Task OffboardAsync_Always_RemovesTheAuditorRoleTheOrganizationChangeWouldLeaveBehind()
    {
        // Arrange
        var request = CreateRequest(null);
        GivenUser(request.UserId, _organizationId);

        // Act
        await _sut.OffboardAsync(request, CancellationToken.None);

        // Assert
        _mockApplicationUserService.Verify(
            x => x.RemoveRoleAsync(request.UserId, UserRole.Auditor, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OffboardAsync_Always_WithdrawsTheOpenInvitationsTheLeaverIssued()
    {
        // Arrange
        var request = CreateRequest(null);
        GivenUser(request.UserId, _organizationId);

        _mockInvitationRepository
            .Setup(x => x.RevokePendingByInviterAsync(_organizationId, request.UserId, request.OffboardedByUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Count(2));

        // Act
        var offboarded = await _sut.OffboardAsync(request, CancellationToken.None);

        // Assert
        ((int)offboarded.RevokedInvitationCount).Should().Be(2);
    }

    [Fact]
    public async Task OffboardAsync_TheCallerIsThePersonLeaving_Throws()
    {
        // Arrange
        var userId = FakeApplicationUserId.CreateValid(Fixture);

        var request = new OffboardUserRequest
        {
            OrganizationId = _organizationId,
            UserId = userId,
            SuccessorUserId = null,
            OffboardedByUserId = userId
        };

        // Act
        var act = () => _sut.OffboardAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOffboardingException>();
    }

    [Fact]
    public async Task OffboardAsync_TheSuccessorIsThePersonLeaving_Throws()
    {
        // Arrange
        var request = CreateRequest(null);
        request = request with { SuccessorUserId = request.UserId };

        // Act
        var act = () => _sut.OffboardAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOffboardingException>();
    }

    [Fact]
    public async Task OffboardAsync_TheSuccessorBelongsToAnotherOrganization_ThrowsBeforeAnythingMoves()
    {
        // Arrange
        var request = CreateRequest(FakeApplicationUserId.CreateValid(Fixture));
        GivenUser(request.UserId, _organizationId);
        GivenUser(request.SuccessorUserId!.Value, FakeOrganizationId.CreateValid(Fixture));

        // Act
        var act = () => _sut.OffboardAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOffboardingException>();

        _mockCaseRepository.Verify(
            x => x.ReassignOpenCasesAsync(It.IsAny<OrganizationId>(), It.IsAny<ApplicationUserId>(), It.IsAny<ApplicationUserId?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private OffboardUserRequest CreateRequest(ApplicationUserId? successorUserId)
    {
        return new OffboardUserRequest
        {
            OrganizationId = _organizationId,
            UserId = FakeApplicationUserId.CreateValid(Fixture),
            SuccessorUserId = successorUserId,
            OffboardedByUserId = FakeApplicationUserId.CreateValid(Fixture)
        };
    }

    private ApplicationUser GivenUser(ApplicationUserId userId, OrganizationId organizationId)
    {
        var user = FakeApplicationUser.CreateValid(Fixture) with
        {
            UserId = userId,
            OrganizationId = organizationId,
            SubOrganizationId = null
        };

        _mockApplicationUserService
            .Setup(x => x.GetAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        return user;
    }
}
