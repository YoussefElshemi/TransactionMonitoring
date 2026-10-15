using System.Net;
using Core.Constants;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.WebhookConfigurations;
using Presentation.Validators.Webhooks;

namespace Presentation.Validators.OrganizationWebhooks;

public class UpdateOrganizationWebhookConfigurationRequestValidator : AbstractValidator<UpdateOrganizationWebhookConfigurationRequestDto>
{
    public UpdateOrganizationWebhookConfigurationRequestValidator(IOrganizationService organizationService, IWebhookConfigurationService webhookConfigurationService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await organizationService.ExistsAsync(new OrganizationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.WebhookConfigurationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await webhookConfigurationService.ExistsAsync(new OrganizationId(x.OrganizationId), new WebhookConfigurationId(x.WebhookConfigurationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Webhook configuration not found");

        RuleFor(x => x.Body.EndpointUrl)
            .Must(BeAUri!)
            .WithMessage("EndpointUrl must be a valid URI.")
            .When(x => x.Body.EndpointUrl != null);

        RuleForEach(x => x.Body.Events!)
            .Must(WebhookEventRules.BeASubscribableEvent)
            .WithMessage(CreateWebhookConfigurationRequestValidator.SubscribableMessage)
            .When(x => x.Body.Events != null);

        RuleFor(x => x.Body.PayloadVersion!.Value)
            .Must(WebhookPayloadVersions.Supported.Contains)
            .WithMessage(CreateWebhookConfigurationRequestValidator.VersionMessage)
            .When(x => x.Body.PayloadVersion.HasValue);
    }

    private static bool BeAUri(string endpointUrl)
    {
        return !string.IsNullOrWhiteSpace(endpointUrl) && Uri.TryCreate(endpointUrl, UriKind.Absolute, out _);
    }
}