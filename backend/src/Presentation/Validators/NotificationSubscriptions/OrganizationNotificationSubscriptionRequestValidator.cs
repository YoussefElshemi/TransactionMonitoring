using System.Net;
using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.Common;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.NotificationSubscriptions;

namespace Presentation.Validators.NotificationSubscriptions;

public class OrganizationNotificationSubscriptionRequestValidator : AbstractValidator<OrganizationNotificationSubscriptionRequestDto>
{
    public OrganizationNotificationSubscriptionRequestValidator(
        IOrganizationService organizationService,
        INotificationSubscriptionService notificationSubscriptionService)
    {
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.NotificationSubscriptionId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await notificationSubscriptionService.ExistsAsync(
                new OrganizationId(x.OrganizationId), new NotificationSubscriptionId(x.NotificationSubscriptionId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Notification Subscription not found");
    }
}

public class OrganizationCreateNotificationSubscriptionRequestValidator : AbstractValidator<OrganizationCreateNotificationSubscriptionRequestDto>
{
    public OrganizationCreateNotificationSubscriptionRequestValidator(IOrganizationService organizationService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.Body.Channel)
            .IsInEnum();

        RuleFor(x => x.Body.Channel)
            .NotEqual(NotificationChannel.Webhook)
            .WithMessage("Webhook deliveries are configured as webhook configurations, not notification subscriptions");

        RuleFor(x => x.Body.Destination)
            .NotEmpty()
            .MaximumLength(512);

        RuleFor(x => x.Body.Destination)
            .EmailAddress()
            .WithMessage("Destination must be an email address for the Email channel")
            .When(x => x.Body.Channel == NotificationChannel.Email);

        RuleFor(x => x.Body.Destination)
            .Must(BeAnAbsoluteHttpUrl)
            .WithMessage("Destination must be an absolute http or https URL for the Chat channel")
            .When(x => x.Body.Channel == NotificationChannel.Chat);

        RuleFor(x => x.Body.MinimumSeverity)
            .IsInEnum()
            .When(x => x.Body.MinimumSeverity.HasValue);

        RuleFor(x => x.Body)
            .Must(body => body.MinimumSeverity.HasValue || body.NotifyOnBreach)
            .WithMessage("A subscription must set a minimum severity, opt in to breach notifications, or both");

        RuleFor(x => x.Body.Locale)
            .Must(Locale.IsKnown!)
            .WithMessage("'Locale' must be a language tag this host knows, such as 'fr-FR'.")
            .When(x => !string.IsNullOrWhiteSpace(x.Body.Locale));
    }

    private static bool BeAnAbsoluteHttpUrl(string destination)
    {
        return Uri.TryCreate(destination, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}

public class OrganizationUpdateNotificationSubscriptionRequestValidator : AbstractValidator<OrganizationUpdateNotificationSubscriptionRequestDto>
{
    public OrganizationUpdateNotificationSubscriptionRequestValidator(
        IOrganizationService organizationService,
        INotificationSubscriptionService notificationSubscriptionService)
    {
        // Cascade stop: the rules below read the subscription's channel.
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.NotificationSubscriptionId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await notificationSubscriptionService.ExistsAsync(
                new OrganizationId(x.OrganizationId), new NotificationSubscriptionId(x.NotificationSubscriptionId), new IncludeDeleted(false), ct))
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

        async Task<bool> HasChannelAsync(
            OrganizationUpdateNotificationSubscriptionRequestDto dto,
            NotificationChannel channel,
            CancellationToken stoppingToken)
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
