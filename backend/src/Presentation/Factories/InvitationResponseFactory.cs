using Core.Configs;
using Core.Interfaces.Services;
using Core.Models.Invitations;
using Core.ValueObjects;
using Microsoft.Extensions.Options;
using Presentation.Mappers.Invitations;
using Presentation.Models.Invitations;
using Presentation.Models.Users;

namespace Presentation.Factories;

public class InvitationResponseFactory(
    RelatedRecordLoader relatedRecordLoader,
    IApplicationUserService applicationUserService,
    IOptions<AppConfig> appConfig,
    TimeProvider timeProvider)
{
    public async Task<InvitationResourceDto> CreateAsync(
        Invitation invitation, EnrichmentContext context, CancellationToken stoppingToken)
    {
        var resources = await CreateAsync([invitation], context, stoppingToken);

        return resources[0];
    }

    public async Task<InvitationResourceDto[]> CreateAsync(
        IReadOnlyCollection<Invitation> invitations, EnrichmentContext context, CancellationToken stoppingToken)
    {
        if (invitations.Count == 0)
        {
            return [];
        }

        var now = timeProvider.GetUtcNow();

        var organizations = await relatedRecordLoader.LoadOrganizationsAsync(
            invitations.Select(x => (OrganizationId?)x.OrganizationId), context, stoppingToken);

        var users = await relatedRecordLoader.LoadUsersAsync(
            invitations.Select(x => (ApplicationUserId?)x.InvitedByUserId)
                .Concat(invitations.Select(x => x.AcceptedByUserId)), context, stoppingToken);

        var inviterOrganizations = await ResolveInviterOrganizationsAsync(invitations, users, stoppingToken);

        return invitations.Select(x => new InvitationResourceDto
        {
            Invitation = x.ToDto(now, IsPlatformOperatorIssued(x, inviterOrganizations)),
            Organization = organizations.GetValueOrDefault(x.OrganizationId),
            InvitedByUser = users.GetValueOrDefault(x.InvitedByUserId),
            AcceptedByUser = x.AcceptedByUserId.HasValue ? users.GetValueOrDefault(x.AcceptedByUserId.Value) : null
        }).ToArray();
    }

    public async Task<IssuedInvitationResourceDto> CreateAsync(
        IssuedInvitation issuedInvitation, EnrichmentContext context, CancellationToken stoppingToken)
    {
        var invitation = issuedInvitation.Invitation;

        var organizations = await relatedRecordLoader.LoadOrganizationsAsync(
            [invitation.OrganizationId], context, stoppingToken);

        var users = await relatedRecordLoader.LoadUsersAsync(
            [invitation.InvitedByUserId], context, stoppingToken);

        return new IssuedInvitationResourceDto
        {
            Invitation = issuedInvitation.ToDto(
                appConfig.Value.InvitationConfig.AcceptUrl,
                timeProvider.GetUtcNow(),
                context.OrganizationId != invitation.OrganizationId),
            Organization = organizations.GetValueOrDefault(invitation.OrganizationId),
            InvitedByUser = users.GetValueOrDefault(invitation.InvitedByUserId)
        };
    }

    private static bool IsPlatformOperatorIssued(
        Invitation invitation, IReadOnlyDictionary<ApplicationUserId, OrganizationId> inviterOrganizations)
    {
        return inviterOrganizations.TryGetValue(invitation.InvitedByUserId, out var organizationId) &&
               organizationId != invitation.OrganizationId;
    }

    private async Task<IReadOnlyDictionary<ApplicationUserId, OrganizationId>> ResolveInviterOrganizationsAsync(
        IReadOnlyCollection<Invitation> invitations,
        IReadOnlyDictionary<ApplicationUserId, UserResponseDto> users,
        CancellationToken stoppingToken)
    {
        var organizations = new Dictionary<ApplicationUserId, OrganizationId>();

        foreach (var (userId, user) in users)
        {
            organizations[userId] = new OrganizationId(user.OrganizationId);
        }

        var withheld = invitations
            .Select(x => x.InvitedByUserId)
            .Where(x => !organizations.ContainsKey(x))
            .Distinct()
            .ToList();

        if (withheld.Count == 0)
        {
            return organizations;
        }

        var resolved = await applicationUserService.ListByIdsAsync(withheld, stoppingToken);

        foreach (var user in resolved)
        {
            organizations[user.UserId] = user.OrganizationId;
        }

        return organizations;
    }
}
