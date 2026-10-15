using Core.Configs;
using Core.Helpers;
using Core.Interfaces.Services;
using Core.Localization;
using Core.Models.Invitations;
using Core.ValueObjects;
using Microsoft.Extensions.Options;
using Serilog;

namespace Infrastructure.Services;

public class EmailInvitationNotifier(
    IEmailSender emailSender,
    IOrganizationService organizationService,
    IApplicationUserService applicationUserService,
    IDisplayPreferenceService displayPreferenceService,
    IOptions<AppConfig> appConfig,
    TimeProvider timeProvider,
    ILogger logger) : IInvitationNotifier
{
    public async Task NotifyAsync(IssuedInvitation issuedInvitation, CancellationToken stoppingToken)
    {
        var invitation = issuedInvitation.Invitation;

        try
        {
            var organization = await organizationService.GetAsync(invitation.OrganizationId, stoppingToken);
            var invitedBy = await applicationUserService.GetAsync(invitation.InvitedByUserId, stoppingToken);

            var translator = new Translator(
                await displayPreferenceService.ForOrganizationAsync(invitation.OrganizationId, stoppingToken));

            var notificationMessage = InvitationNotificationMessageBuilder.ForInvitation(
                invitation,
                organization,
                invitedBy.UserName?.ToString() ?? invitedBy.Email?.ToString() ?? organization.FriendlyName.ToString(),
                InvitationLinkBuilder.Build(appConfig.Value.InvitationConfig.AcceptUrl, issuedInvitation.InvitationToken),
                translator,
                timeProvider.GetUtcNow());

            await emailSender.SendAsync(new Destination(invitation.Email), notificationMessage, stoppingToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.Warning(
                exception,
                "Sending invitation {InvitationId} failed: {Message}",
                invitation.InvitationId.ToString(),
                exception.Message);
        }
    }
}
