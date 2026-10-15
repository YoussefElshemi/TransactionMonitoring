using Core.Constants;
using Core.Enums;
using Core.Exceptions;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.Users;
using Core.ValueObjects;

namespace Core.Services;

public class OffboardingService(
    IApplicationUserService applicationUserService,
    ICaseRepository caseRepository,
    IQueueMemberRepository queueMemberRepository,
    IRoleAssignmentRepository roleAssignmentRepository,
    IInvitationRepository invitationRepository,
    IUnitOfWork unitOfWork) : IOffboardingService
{
    public async Task<OffboardedUser> OffboardAsync(OffboardUserRequest offboardUserRequest, CancellationToken stoppingToken)
    {
        await EnsureValidAsync(offboardUserRequest, stoppingToken);

        var reassignedCaseCount = await caseRepository.ReassignOpenCasesAsync(
            offboardUserRequest.OrganizationId,
            offboardUserRequest.UserId,
            offboardUserRequest.SuccessorUserId,
            stoppingToken);

        var queueMembershipCount = await queueMemberRepository.ReplaceMemberAsync(
            offboardUserRequest.OrganizationId,
            offboardUserRequest.UserId,
            offboardUserRequest.SuccessorUserId,
            stoppingToken);

        var revokedRoleAssignmentCount = await roleAssignmentRepository.DeleteForUserAsync(
            offboardUserRequest.UserId, stoppingToken);

        var revokedInvitationCount = await invitationRepository.RevokePendingByInviterAsync(
            offboardUserRequest.OrganizationId,
            offboardUserRequest.UserId,
            offboardUserRequest.OffboardedByUserId,
            stoppingToken);

        await unitOfWork.SaveChangesAsync(stoppingToken);

        await ReleaseAsync(offboardUserRequest.UserId, stoppingToken);

        return new OffboardedUser
        {
            UserId = offboardUserRequest.UserId,
            SuccessorUserId = offboardUserRequest.SuccessorUserId,
            ReassignedCaseCount = reassignedCaseCount,
            QueueMembershipCount = queueMembershipCount,
            RevokedRoleAssignmentCount = revokedRoleAssignmentCount,
            RevokedInvitationCount = revokedInvitationCount
        };
    }

    private async Task ReleaseAsync(ApplicationUserId userId, CancellationToken stoppingToken)
    {
        var user = await applicationUserService.GetAsync(userId, stoppingToken);

        if (user.SubOrganizationId.HasValue)
        {
            await applicationUserService.SetSubOrganizationAsync(userId, null, stoppingToken);
        }

        // The user update removes Manager on an organisation change but not Auditor.
        await applicationUserService.RemoveRoleAsync(userId, UserRole.Auditor, stoppingToken);

        user.OrganizationId = new OrganizationId(SystemConstants.DefaultOrganization);

        await applicationUserService.UpdateAsync(user, stoppingToken);
    }

    private async Task EnsureValidAsync(OffboardUserRequest offboardUserRequest, CancellationToken stoppingToken)
    {
        if (offboardUserRequest.UserId == offboardUserRequest.OffboardedByUserId)
        {
            throw InvalidOffboardingException.Self();
        }

        if (offboardUserRequest.SuccessorUserId is not { } successorUserId)
        {
            return;
        }

        if (successorUserId == offboardUserRequest.UserId)
        {
            throw InvalidOffboardingException.SuccessorIsLeaver();
        }

        var successor = await applicationUserService.GetAsync(successorUserId, stoppingToken);

        if (successor.OrganizationId != offboardUserRequest.OrganizationId)
        {
            throw InvalidOffboardingException.SuccessorOutsideOrganization();
        }
    }
}
