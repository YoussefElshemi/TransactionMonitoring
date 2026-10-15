using System.Net;
using Core.Constants;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.WebhookConfigurations;
using Presentation.Validators.Webhooks;

namespace Presentation.Validators.OrganizationWebhooks;

public class CreateWebhookConfigurationRequestValidator : AbstractValidator<CreateWebhookConfigurationRequestDto>
{
    public CreateWebhookConfigurationRequestValidator(IOrganizationService organizationService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.Body.EndpointUrl)
            .Must(BeAUri)
            .WithMessage("EndpointUrl must be a valid URI.");

        RuleFor(x => x.Body.IsEnabled)
            .NotNull();

        RuleForEach(x => x.Body.Events!)
            .Must(WebhookEventRules.BeASubscribableEvent)
            .WithMessage(SubscribableMessage)
            .When(x => x.Body.Events != null);

        RuleFor(x => x.Body.PayloadVersion!.Value)
            .Must(WebhookPayloadVersions.Supported.Contains)
            .WithMessage(VersionMessage)
            .When(x => x.Body.PayloadVersion.HasValue);
    }

    internal static readonly string SubscribableMessage =
        $"Events must be published, subscribable webhook events: {string.Join(", ", WebhookEvents.Subscribable.Order(StringComparer.Ordinal))}.";

    internal static readonly string VersionMessage =
        $"PayloadVersion must be one of the supported webhook payload versions: {string.Join(", ", WebhookPayloadVersions.Supported.Order())}.";

    private static bool BeAUri(string endpointUrl)
    {
        return !string.IsNullOrWhiteSpace(endpointUrl) && Uri.TryCreate(endpointUrl, UriKind.Absolute, out _);
    }
}