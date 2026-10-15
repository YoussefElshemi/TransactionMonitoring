using AutoFixture;
using Core.Configs;
using Core.Constants;
using Core.Enums;
using Core.Exceptions;
using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.Invitations;
using Core.Models.Permissions;
using Core.Models.Users;
using Core.Services;
using Core.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Configs;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Invitations;
using UnitTests.TestHelpers.FakeObjects.Core.Models.Users;

namespace UnitTests.Tests.Core.Services;

public class InvitationServiceTests : BaseTestClass
{
    private const int ExpiryDays = 14;

    private readonly Mock<IInvitationRepository> _mockInvitationRepository = new();
    private readonly Mock<IApplicationUserService> _mockApplicationUserService = new();
    private readonly Mock<IPermissionService> _mockPermissionService = new();
    private readonly Mock<IUnitOfWork> _mockUnitOfWork = new();
    private readonly InvitationService _sut;

    public InvitationServiceTests()
    {
        var appConfig = FakeAppConfig.CreateValid(Fixture) with
        {
            InvitationConfig = new InvitationConfig { ExpiryDays = ExpiryDays, AcceptUrl = "https://console.test/invitations/accept" }
        };

        var mockConfig = new Mock<IOptions<AppConfig>>();
        mockConfig.SetupGet(x => x.Value).Returns(appConfig);

        _sut = new InvitationService(
            _mockInvitationRepository.Object,
            _mockApplicationUserService.Object,
            _mockPermissionService.Object,
            _mockUnitOfWork.Object,
            mockConfig.Object,
            MockTimeProvider);
    }

    [Fact]
    public async Task InviteAsync_InviterHoldsTheSeat_StoresOnlyTheDigestOfTheTokenItReturns()
    {
        // Arrange
        var request = FakeCreateInvitationRequest.CreateValid(Fixture);
        GivenInviterHolds(request.InvitedByUserId, UserRole.Manager);

        Hash? persisted = null;

        _mockInvitationRepository
            .Setup(x => x.AddAsync(It.IsAny<Invitation>(), It.IsAny<Hash>(), It.IsAny<CancellationToken>()))
            .Callback<Invitation, Hash, CancellationToken>((_, hash, _) => persisted = hash);

        // Act
        var issued = await _sut.InviteAsync(request, CancellationToken.None);

        // Assert
        persisted.Should().NotBeNull();
        ((string)persisted!.Value).Should().Be(InvitationTokenHelper.Hash(issued.InvitationToken));
        ((string)persisted.Value).Should().NotBe(issued.InvitationToken);
    }

    [Fact]
    public async Task InviteAsync_Always_ExpiresAfterTheConfiguredWindow()
    {
        // Arrange
        var request = FakeCreateInvitationRequest.CreateValid(Fixture);
        GivenInviterHolds(request.InvitedByUserId, UserRole.Manager);

        // Act
        var issued = await _sut.InviteAsync(request, CancellationToken.None);

        // Assert
        ((DateTimeOffset)issued.Invitation.ExpiresAt).Should().Be(TestTimeConstants.Today.AddDays(ExpiryDays));
    }

    [Fact]
    public async Task InviteAsync_AnOfferToThatAddressIsAlreadyOpen_ReissuesItRatherThanAddingASecond()
    {
        // Arrange
        var request = FakeCreateInvitationRequest.CreateValid(Fixture);
        var pending = FakeInvitation.CreateValid(Fixture);

        GivenInviterHolds(request.InvitedByUserId, UserRole.Manager);

        _mockInvitationRepository
            .Setup(x => x.FindPendingByEmailAsync(request.OrganizationId, request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pending);

        _mockInvitationRepository
            .Setup(x => x.GetAsync(pending.InvitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pending);

        // Act
        var issued = await _sut.InviteAsync(request, CancellationToken.None);

        // Assert
        issued.Invitation.InvitationId.Should().Be(pending.InvitationId);
        _mockInvitationRepository.Verify(
            x => x.ReissueAsync(pending.InvitationId, It.IsAny<Hash>(), It.IsAny<ExpiresAt>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockInvitationRepository.Verify(
            x => x.AddAsync(It.IsAny<Invitation>(), It.IsAny<Hash>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InviteAsync_InviterDoesNotHoldEverythingTheSeatHolds_Throws()
    {
        // Arrange
        var request = FakeCreateInvitationRequest.CreateValid(Fixture) with { UserRole = UserRole.Manager };
        GivenInviterHolds(request.InvitedByUserId, UserRole.User);

        // Act
        var act = () => _sut.InviteAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<PermissionNotGrantableException>();
    }

    [Fact]
    public async Task InviteAsync_SeatIsTheInstallationOperators_ThrowsEvenForAnAdmin()
    {
        // Arrange
        var request = FakeCreateInvitationRequest.CreateValid(Fixture) with { UserRole = UserRole.Admin };
        GivenInviterHolds(request.InvitedByUserId, UserRole.Admin);

        // Act
        var act = () => _sut.InviteAsync(request, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<PermissionNotGrantableException>();
        exception.Which.Permissions.Should().Contain(Permission.PlatformAdminister);
    }

    [Fact]
    public async Task InviteAsync_AddressAlreadyBelongsToAnOrganization_Throws()
    {
        // Arrange
        var request = FakeCreateInvitationRequest.CreateValid(Fixture);
        GivenInviterHolds(request.InvitedByUserId, UserRole.Manager);

        _mockApplicationUserService
            .Setup(x => x.FindByEmailAsync(request.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeApplicationUser.CreateValid(Fixture));

        // Act
        var act = () => _sut.InviteAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvitationAlreadyMemberException>();
    }

    [Fact]
    public async Task AcceptAsync_TokenIsUnknown_Throws()
    {
        // Arrange
        _mockInvitationRepository
            .Setup(x => x.FindByTokenAsync(It.IsAny<Hash>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Invitation?)null);

        // Act
        var act = () => _sut.AcceptAsync(CreateAcceptRequest(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidInvitationTokenException>();
    }

    [Fact]
    public async Task AcceptAsync_InvitationHasExpired_ThrowsTheSameAsAnUnknownToken()
    {
        // Arrange
        GivenInvitationForToken(FakeInvitation.CreateValid(Fixture) with
        {
            ExpiresAt = new ExpiresAt(TestTimeConstants.Today.AddDays(-1))
        });

        // Act
        var act = () => _sut.AcceptAsync(CreateAcceptRequest(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidInvitationTokenException>();
    }

    [Fact]
    public async Task AcceptAsync_InvitationWasRevoked_ThrowsTheSameAsAnUnknownToken()
    {
        // Arrange
        GivenInvitationForToken(FakeInvitation.CreateValid(Fixture) with
        {
            RevokedAt = new RevokedAt(TestTimeConstants.Today)
        });

        // Act
        var act = () => _sut.AcceptAsync(CreateAcceptRequest(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidInvitationTokenException>();
    }

    [Fact]
    public async Task AcceptAsync_InvitationWasAlreadyTakenUp_ThrowsTheSameAsAnUnknownToken()
    {
        // Arrange
        GivenInvitationForToken(FakeInvitation.CreateValid(Fixture) with
        {
            AcceptedAt = new AcceptedAt(TestTimeConstants.Today),
            AcceptedByUserId = FakeApplicationUser.CreateValid(Fixture).UserId
        });

        // Act
        var act = () => _sut.AcceptAsync(CreateAcceptRequest(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidInvitationTokenException>();
    }

    [Fact]
    public async Task AcceptAsync_AddressHasNoAccount_CreatesOneInTheInvitingOrganizationWithItsEmailConfirmed()
    {
        // Arrange
        var invitation = FakeInvitation.CreateValid(Fixture) with { UserRole = UserRole.Manager };
        GivenInvitationForToken(invitation);

        CreateApplicationUserRequest? created = null;

        _mockApplicationUserService
            .Setup(x => x.CreateAsync(It.IsAny<CreateApplicationUserRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CreateApplicationUserRequest, CancellationToken>((request, _) => created = request);

        // Act
        var accepted = await _sut.AcceptAsync(CreateAcceptRequest(), CancellationToken.None);

        // Assert
        created.Should().NotBeNull();
        created!.OrganizationId.Should().Be(invitation.OrganizationId);
        ((string)created.Email).Should().Be(invitation.Email);
        ((bool)created.EmailConfirmed).Should().BeTrue();
        ((bool)accepted.IsCreated).Should().BeTrue();

        _mockApplicationUserService.Verify(
            x => x.AddRoleAsync(created.UserId, UserRole.Manager, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AcceptAsync_AddressHasNoAccountAndNoPasswordWasGiven_ThrowsTheSameAsAnUnknownToken()
    {
        // Arrange
        GivenInvitationForToken(FakeInvitation.CreateValid(Fixture));

        var request = CreateAcceptRequest() with { Password = null };

        // Act
        var act = () => _sut.AcceptAsync(request, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidInvitationTokenException>();
    }

    [Fact]
    public async Task AcceptAsync_AddressHasAnAccountWaitingInTheDefaultOrganization_AttachesItRatherThanCreatingASecond()
    {
        // Arrange
        var invitation = FakeInvitation.CreateValid(Fixture);
        GivenInvitationForToken(invitation);

        var waiting = FakeApplicationUser.CreateValid(Fixture) with
        {
            OrganizationId = new OrganizationId(SystemConstants.DefaultOrganization)
        };

        _mockApplicationUserService
            .Setup(x => x.FindByEmailAsync(invitation.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(waiting);

        // Act
        var accepted = await _sut.AcceptAsync(CreateAcceptRequest(), CancellationToken.None);

        // Assert
        accepted.UserId.Should().Be(waiting.UserId);
        ((bool)accepted.IsCreated).Should().BeFalse();
        waiting.OrganizationId.Should().Be(invitation.OrganizationId);

        _mockApplicationUserService.Verify(
            x => x.CreateAsync(It.IsAny<CreateApplicationUserRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AcceptAsync_AddressAlreadyBelongsToAnotherOrganization_Throws()
    {
        // Arrange
        var invitation = FakeInvitation.CreateValid(Fixture);
        GivenInvitationForToken(invitation);

        _mockApplicationUserService
            .Setup(x => x.FindByEmailAsync(invitation.Email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeApplicationUser.CreateValid(Fixture));

        // Act
        var act = () => _sut.AcceptAsync(CreateAcceptRequest(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvitationAlreadyMemberException>();
    }

    [Fact]
    public async Task ResendAsync_InvitationHasLapsed_ReissuesWithAFreshTokenAndWindow()
    {
        // Arrange
        var invitation = FakeInvitation.CreateValid(Fixture) with
        {
            ExpiresAt = new ExpiresAt(TestTimeConstants.Today.AddDays(-1))
        };

        _mockInvitationRepository
            .Setup(x => x.GetAsync(invitation.InvitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);

        ExpiresAt? reissuedExpiry = null;

        _mockInvitationRepository
            .Setup(x => x.ReissueAsync(invitation.InvitationId, It.IsAny<Hash>(), It.IsAny<ExpiresAt>(), It.IsAny<CancellationToken>()))
            .Callback<InvitationId, Hash, ExpiresAt, CancellationToken>((_, _, expiresAt, _) => reissuedExpiry = expiresAt);

        // Act
        var issued = await _sut.ResendAsync(invitation.InvitationId, CancellationToken.None);

        // Assert
        ((string)issued.InvitationToken).Should().NotBeNullOrWhiteSpace();
        reissuedExpiry.Should().NotBeNull();
        ((DateTimeOffset)reissuedExpiry!.Value).Should().Be(TestTimeConstants.Today.AddDays(ExpiryDays));
    }

    [Fact]
    public async Task ResendAsync_InvitationWasAlreadyTakenUp_Throws()
    {
        // Arrange
        var invitation = FakeInvitation.CreateValid(Fixture) with
        {
            AcceptedAt = new AcceptedAt(TestTimeConstants.Today),
            AcceptedByUserId = FakeApplicationUser.CreateValid(Fixture).UserId
        };

        _mockInvitationRepository
            .Setup(x => x.GetAsync(invitation.InvitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);

        // Act
        var act = () => _sut.ResendAsync(invitation.InvitationId, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<InvitationNotPendingException>();
        exception.Which.InvitationStatus.Should().Be(InvitationStatus.Accepted);
    }

    [Fact]
    public async Task RevokeAsync_InvitationHasLapsedButWasNeverTakenUp_StillWithdrawsIt()
    {
        // Arrange
        var invitation = FakeInvitation.CreateValid(Fixture) with
        {
            ExpiresAt = new ExpiresAt(TestTimeConstants.Today.AddDays(-1))
        };

        var revokedBy = FakeApplicationUser.CreateValid(Fixture).UserId;

        _mockInvitationRepository
            .Setup(x => x.GetAsync(invitation.InvitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);

        // Act
        await _sut.RevokeAsync(invitation.InvitationId, revokedBy, CancellationToken.None);

        // Assert
        _mockInvitationRepository.Verify(
            x => x.RevokeAsync(invitation.InvitationId, revokedBy, It.IsAny<CancellationToken>()), Times.Once);
    }

    private AcceptInvitationRequest CreateAcceptRequest()
    {
        return new AcceptInvitationRequest
        {
            InvitationToken = InvitationTokenHelper.Generate(),
            UserName = null,
            Password = new Password(Fixture.Create<string>())
        };
    }

    private void GivenInvitationForToken(Invitation invitation)
    {
        _mockInvitationRepository
            .Setup(x => x.FindByTokenAsync(It.IsAny<Hash>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);

        _mockInvitationRepository
            .Setup(x => x.GetAsync(invitation.InvitationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);
    }

    private void GivenInviterHolds(ApplicationUserId userId, UserRole userRole)
    {
        _mockPermissionService
            .Setup(x => x.ResolveAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EffectivePermissions
            {
                UserId = userId,
                OrganizationId = FakeInvitation.CreateValid(Fixture).OrganizationId,
                SubOrganizationId = null,
                Grants = PermissionBundle.For(userRole)
                    .Select(x => new PermissionGrant { Permission = x, Scope = PermissionScope.Organization() })
                    .ToList()
            });
    }
}
