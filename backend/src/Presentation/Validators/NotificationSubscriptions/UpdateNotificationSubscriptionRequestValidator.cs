using System.Net;
using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.Common;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.NotificationSubscriptions;

namespace Presentation.Validators.NotificationSubscriptions;

public class UpdateNotificationSubscriptionRequestValidator : AbstractValidator<UpdateNotificationSubscriptionRequestDto>
{
    public UpdateNotificationSubscriptionRequestValidator(INotificationSubscriptionService notificationSubscriptionService)
    {
        // Cascade stop: the rules below read the subscription's channel.
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.NotificationSubscriptionId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await notificationSubscriptionService.ExistsAsync(new OrganizationId(x.OrganizationId), new NotificationSubscriptionId(x.NotificationSubscriptionId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Notification Subscription not found");

        RuleFor(x => x.Body.Destination)
            .MaximumLength(512)
            .When(x => x.Body.Destination != null);

        RuleFor(x => x.Body.Destination)
            .EmailAddress()
            .WithMessage("Destination must be an email address for the Email channel")
            .WhenAsync(async (x, ct) => x.Body.Destination != null && await HasChannelAsync(x, NotificationChannel.Email, ct));

        RuleFor(x => x.Body.Destination)
            .Must(BeAnAbsoluteHttpUrl!)
            .WithMessage("Destination must be an absolute http or https URL for the Chat channel")
            .WhenAsync(async (x, ct) => x.Body.Destination != null && await HasChannelAsync(x, NotificationChannel.Chat, ct));

        RuleFor(x => x.Body.MinimumSeverity)
            .IsInEnum()
            .When(x => x.Body.MinimumSeverity.HasValue);

        RuleFor(x => x.Body.Locale)
            .Must(BeLocaleKnown)
            .WithMessage("'Locale' must be a language tag this host knows, such as 'fr-FR'.")
            .When(x => x.Body.Locale is { IsSet: true, Value: not null });

        return;

        async Task<bool> HasChannelAsync(UpdateNotificationSubscriptionRequestDto dto, NotificationChannel channel, CancellationToken stoppingToken)
        {
            var notificationSubscription = await notificationSubscriptionService.GetAsync(
                new NotificationSubscriptionId(dto.NotificationSubscriptionId), stoppingToken);

            return notificationSubscription.Channel == channel;
        }
    }

    private static bool BeLocaleKnown(Patch<string?> locale)
    {
        return Locale.IsKnown(locale.Value!);
    }

    private static bool BeAnAbsoluteHttpUrl(string destination)
    {
        return Uri.TryCreate(destination, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
