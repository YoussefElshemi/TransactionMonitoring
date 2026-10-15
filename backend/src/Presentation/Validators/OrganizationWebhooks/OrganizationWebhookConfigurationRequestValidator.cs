using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.WebhookConfigurations;

namespace Presentation.Validators.OrganizationWebhooks;

public class OrganizationWebhookConfigurationRequestValidator : AbstractValidator<OrganizationWebhookConfigurationRequestDto>
{
    public OrganizationWebhookConfigurationRequestValidator(IOrganizationService organizationService, IWebhookConfigurationService webhookConfigurationService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.WebhookConfigurationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await webhookConfigurationService.ExistsAsync(new OrganizationId(x.OrganizationId), new WebhookConfigurationId(x.WebhookConfigurationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Webhook Configuration not found");
    }
}