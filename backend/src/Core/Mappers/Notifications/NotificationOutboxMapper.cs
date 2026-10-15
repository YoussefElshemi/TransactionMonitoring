using Core.Extensions;
using Core.Localization;
using Core.Models.Localization;
using Core.Models.Notifications;
using Core.Models.NotificationSubscriptions;
using Core.Models.OutboxMessages;
using Core.ValueObjects;

namespace Core.Mappers.Notifications;

public static class NotificationOutboxMapper
{
    public static List<CreateOutboxMessageRequest> ToOutboxMessageRequests(
        this List<NotificationSubscription> notificationSubscriptions,
        DisplayPreferences organizationPreferences,
        Func<Translator, NotificationMessage> render)
    {
        var renderedByLocale = new Dictionary<string, Payload>(StringComparer.Ordinal);

        return notificationSubscriptions.ConvertAll(x =>
        {
            var preferences = x.Locale.HasValue
                ? organizationPreferences with { Locale = x.Locale.Value }
                : organizationPreferences;

            var locale = (string)preferences.Locale;

            if (!renderedByLocale.TryGetValue(locale, out var payload))
            {
                payload = new Payload(render(new Translator(preferences)).Serialize().ToBase64String());
                renderedByLocale[locale] = payload;
            }

            return new CreateOutboxMessageRequest
            {
                OrganizationId = x.OrganizationId,
                Payload = payload,
                Channel = x.Channel,
                Destination = x.Destination,
                SecretKey = null
            };
        });
    }
}
