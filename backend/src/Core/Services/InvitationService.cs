using Core.Configs;
using Core.Constants;
using Core.Enums;
using Core.Exceptions;
using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.Common;
using Core.Models.Invitations;
using Core.Models.Users;
using Core.ValueObjects;
using Microsoft.Extensions.Options;

namespace Core.Services;

public class InvitationService(
    IInvitationRepository invitationRepository,
    IApplicationUserService applicationUserService,
    IPermissionService permissionService,
    IUnitOfWork unitOfWork,
    IOptions<AppConfig> appConfig,
    TimeProvider timeProvider) : IInvitationService
{
    public async Task<IssuedInvitation> InviteAsync(CreateInvitationRequest createInvitationRequest, CancellationToken stoppingToken)
    {
        await EnsureGrantableAsync(createInvitationRequest.InvitedByUserId, createInvitationRequest.UserRole, stoppingToken);
        await EnsureInvitableAsync(createInvitationRequest.OrganizationId, createInvitationRequest.Email, stoppingToken);

        var now = timeProvider.GetUtcNow();
        var invitationToken = InvitationTokenHelper.Generate();
        var expiresAt = new ExpiresAt(now.AddDays(appConfig.Value.InvitationConfig.ExpiryDays));

        var pending = await invitationRepository.FindPendingByEmailAsync(
            createInvitationRequest.OrganizationId, createInvitationRequest.Email, stoppingToken);

        if (pending is not null)
        {
            await invitationRepository.ReissueAsync(
                pending.InvitationId, InvitationTokenHelper.Hash(invitationToken), expiresAt, stoppingToken);

            await unitOfWork.SaveChangesAsync(stoppingToken);

            return new IssuedInvitation
            {
                Invitation = await invitationRepository.GetAsync(pending.InvitationId, stoppingToken),
                InvitationToken = invitationToken
            };
        }

        var invitation = new Invitation
        {
            InvitationId = new InvitationId(Guid.NewGuid()),
            OrganizationId = createInvitationRequest.OrganizationId,
            Email = createInvitationRequest.Email,
            UserRole = createInvitationRequest.UserRole,
            SubOrganizationId = createInvitationRequest.SubOrganizationId,
            InvitedByUserId = createInvitationRequest.InvitedByUserId,
            ExpiresAt = expiresAt,
            AcceptedAt = null,
            AcceptedByUserId = null,
            RevokedAt = null,
            RevokedByUserId = null,
            CreatedAt = new CreatedAt(now),
            UpdatedAt = new UpdatedAt(now)
        };

        await invitationRepository.AddAsync(invitation, InvitationTokenHelper.Hash(invitationToken), stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return new IssuedInvitation
        {
            Invitation = invitation,
            InvitationToken = invitationToken
        };
    }

    public Task<Invitation> GetAsync(InvitationId invitationId, CancellationToken stoppingToken)
    {
        return invitationRepository.GetAsync(invitationId, stoppingToken);
    }

    public Task<Exists> ExistsAsync(OrganizationId organizationId, InvitationId invitationId, CancellationToken stoppingToken)
    {
        return invitationRepository.ExistsAsync(organizationId, invitationId, stoppingToken);
    }

    public Task<PagedResponse<Invitation>> ListAsync(ListInvitationRequest listInvitationRequest, CancellationToken stoppingToken)
    {
        return invitationRepository.ListAsync(listInvitationRequest, stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListInvitationRequest listInvitationRequest, CancellationToken stoppingToken)
    {
        return invitationRepository.GetCountAsync(listInvitationRequest, stoppingToken);
    }

    public async Task<IssuedInvitation> ResendAsync(InvitationId invitationId, CancellationToken stoppingToken)
    {
        var invitation = await invitationRepository.GetAsync(invitationId, stoppingToken);
        var now = timeProvider.GetUtcNow();
        var status = InvitationStatusResolver.Resolve(invitation, now);

        if (status is InvitationStatus.Accepted or InvitationStatus.Revoked)
        {
            throw new InvitationNotPendingException(status);
        }

        var invitationToken = InvitationTokenHelper.Generate();
        var expiresAt = new ExpiresAt(now.AddDays(appConfig.Value.InvitationConfig.ExpiryDays));

        await invitationRepository.ReissueAsync(invitationId, InvitationTokenHelper.Hash(invitationToken), expiresAt, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return new IssuedInvitation
        {
            Invitation = await invitationRepository.GetAsync(invitationId, stoppingToken),
            InvitationToken = invitationToken
        };
    }

    public async Task RevokeAsync(InvitationId invitationId, ApplicationUserId revokedByUserId, CancellationToken stoppingToken)
    {
        var invitation = await invitationRepository.GetAsync(invitationId, stoppingToken);
        var status = InvitationStatusResolver.Resolve(invitation, timeProvider.GetUtcNow());

        if (status is InvitationStatus.Accepted or InvitationStatus.Revoked)
        {
            throw new InvitationNotPendingException(status);
        }

        await invitationRepository.RevokeAsync(invitationId, revokedByUserId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public Task<Invitation> PreviewAsync(InvitationToken invitationToken, CancellationToken stoppingToken)
    {
        return FindPendingAsync(invitationToken, stoppingToken);
    }

    public async Task<AcceptedInvitation> AcceptAsync(AcceptInvitationRequest acceptInvitationRequest, CancellationToken stoppingToken)
    {
        var invitation = await FindPendingAsync(acceptInvitationRequest.InvitationToken, stoppingToken);
        var existing = await applicationUserService.FindByEmailAsync(invitation.Email, stoppingToken);

        var userId = existing is null
            ? await ProvisionAsync(invitation, acceptInvitationRequest, stoppingToken)
            : await AttachAsync(invitation, existing, stoppingToken);

        await applicationUserService.AddRoleAsync(userId, invitation.UserRole, stoppingToken);

        await invitationRepository.AcceptAsync(invitation.InvitationId, userId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return new AcceptedInvitation
        {
            Invitation = await invitationRepository.GetAsync(invitation.InvitationId, stoppingToken),
            UserId = userId,
            IsCreated = new IsCreated(existing is null)
        };
    }

    private async Task<ApplicationUserId> ProvisionAsync(
        Invitation invitation, AcceptInvitationRequest acceptInvitationRequest, CancellationToken stoppingToken)
    {
        if (acceptInvitationRequest.Password is not { } password)
        {
            throw new InvalidInvitationTokenException();
        }

        var userId = new ApplicationUserId(Guid.NewGuid());

        await applicationUserService.CreateAsync(
            new CreateApplicationUserRequest
            {
                UserId = userId,
                OrganizationId = invitation.OrganizationId,
                SubOrganizationId = invitation.SubOrganizationId,
                Email = invitation.Email,
                UserName = acceptInvitationRequest.UserName ?? new UserName(invitation.Email),
                Password = password,

                EmailConfirmed = new EmailConfirmed(true)
            },
            stoppingToken);

        return userId;
    }

    private async Task<ApplicationUserId> AttachAsync(
        Invitation invitation, ApplicationUser existing, CancellationToken stoppingToken)
    {
        if (existing.OrganizationId != new OrganizationId(SystemConstants.DefaultOrganization))
        {
            throw new InvitationAlreadyMemberException(existing.OrganizationId == invitation.OrganizationId);
        }

        existing.OrganizationId = invitation.OrganizationId;

        await applicationUserService.UpdateAsync(existing, stoppingToken);

        if (invitation.SubOrganizationId is { } subOrganizationId)
        {
            await applicationUserService.SetSubOrganizationAsync(existing.UserId, subOrganizationId, stoppingToken);
        }

        return existing.UserId;
    }

    private async Task<Invitation> FindPendingAsync(InvitationToken invitationToken, CancellationToken stoppingToken)
    {
        var invitation = await invitationRepository.FindByTokenAsync(InvitationTokenHelper.Hash(invitationToken), stoppingToken);

        if (invitation is null || !InvitationStatusResolver.IsPending(invitation, timeProvider.GetUtcNow()))
        {
            throw new InvalidInvitationTokenException();
        }

        return invitation;
    }

    private async Task EnsureGrantableAsync(ApplicationUserId invitedByUserId, UserRole userRole, CancellationToken stoppingToken)
    {
        var permissions = PermissionBundle.For(userRole);

        var platformPermissions = permissions.Where(x => !PermissionBundle.IsGrantableToCustomRole(x)).ToList();

        if (platformPermissions.Count > 0)
        {
            throw new PermissionNotGrantableException(platformPermissions, true);
        }

        var inviter = await permissionService.ResolveAsync(invitedByUserId, stoppingToken);

        var notHeld = permissions.Where(x => !inviter.Holds(x)).ToList();

        if (notHeld.Count > 0)
        {
            throw new PermissionNotGrantableException(notHeld, false);
        }
    }

    private async Task EnsureInvitableAsync(OrganizationId organizationId, Email email, CancellationToken stoppingToken)
    {
        var existing = await applicationUserService.FindByEmailAsync(email, stoppingToken);

        if (existing is not null && existing.OrganizationId != new OrganizationId(SystemConstants.DefaultOrganization))
        {
            throw new InvitationAlreadyMemberException(existing.OrganizationId == organizationId);
        }
    }
}
