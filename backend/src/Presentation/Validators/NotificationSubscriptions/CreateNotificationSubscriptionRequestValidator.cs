using System.Net;
using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.NotificationSubscriptions;

namespace Presentation.Validators.NotificationSubscriptions;

public class CreateNotificationSubscriptionRequestValidator : AbstractValidator<CreateNotificationSubscriptionRequestDto>
{
    public CreateNotificationSubscriptionRequestValidator(IOrganizationService organizationService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
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
            .Must(destination => Uri.TryCreate(destination, UriKind.Absolute, out var uri) &&
                                 (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
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
}
