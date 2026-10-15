using Core.Constants;
using Core.Localization;
using Core.Models.Invitations;
using Core.Models.Notifications;
using Core.Models.Organizations;

namespace Core.Helpers;

public static class InvitationNotificationMessageBuilder
{
    public static NotificationMessage ForInvitation(
        Invitation invitation,
        Organization organization,
        string invitedByName,
        string acceptUrl,
        Translator translator,
        DateTimeOffset timestamp)
    {
        return new NotificationMessage
        {
            Subject = translator.Text(TranslationKeys.NotificationInvitationSubject, organization.FriendlyName.ToString()),

            Body = translator.Text(
                TranslationKeys.NotificationInvitationBody,
                invitedByName,
                organization.FriendlyName.ToString(),
                invitation.UserRole.ToString(),
                acceptUrl,
                translator.Moment(invitation.ExpiresAt)),
            CaseId = null,
            Event = WebhookEvents.InvitationSent,
            Timestamp = timestamp
        };
    }
}
