using System.Net;
using Core.Constants;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Webhooks;

namespace Presentation.Validators.Webhooks;

public class ReplayWebhookDeliveryRequestValidator : AbstractValidator<ReplayWebhookDeliveryRequestDto>
{
    public ReplayWebhookDeliveryRequestValidator(IOrganizationService organizationService, IWebhookDeliveryService webhookDeliveryService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.OutboxMessageId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (dto, x, _, ct) =>
                await webhookDeliveryService.ExistsAsync(new OrganizationId(dto.OrganizationId), new OutboxMessageId(x), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Webhook delivery not found");
    }
}

public class ListWebhookDeliveryRequestValidator : AbstractValidator<ListWebhookDeliveryRequestDto>
{
    public ListWebhookDeliveryRequestValidator(IOrganizationService organizationService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.Event!)
            .Must(WebhookEventRules.BeAPublishedEvent)
            .WithMessage(WebhookEventRules.Message)
            .When(x => x.Event != null);
    }
}

public class ListMyWebhookDeliveryRequestValidator : AbstractValidator<ListMyWebhookDeliveryRequestDto>
{
    public ListMyWebhookDeliveryRequestValidator(IOrganizationService organizationService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.Event!)
            .Must(WebhookEventRules.BeAPublishedEvent)
            .WithMessage(WebhookEventRules.Message)
            .When(x => x.Event != null);
    }
}

public class ReplayWebhookDeliveryRangeRequestValidator : AbstractValidator<ReplayWebhookDeliveryRangeRequestDto>
{
    public ReplayWebhookDeliveryRangeRequestValidator(
        IOrganizationService organizationService, IWebhookConfigurationService webhookConfigurationService)
    {
        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");

        RuleFor(x => x.Body.To)
            .GreaterThanOrEqualTo(x => x.Body.From)
            .WithMessage("The replay window must not end before it starts.");

        RuleFor(x => x.Body.WebhookConfigurationId!.Value)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (dto, x, _, ct) => await webhookConfigurationService.ExistsAsync(
                new OrganizationId(dto.OrganizationId), new WebhookConfigurationId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Webhook configuration not found")
            .When(x => x.Body.WebhookConfigurationId.HasValue);

        RuleFor(x => x.Body.Event!)
            .Must(WebhookEventRules.BeAPublishedEvent)
            .WithMessage(WebhookEventRules.Message)
            .When(x => x.Body.Event != null);
    }
}

public static class WebhookEventRules
{
    public static readonly string Message =
        $"Event must be one of the published webhook events: {string.Join(", ", WebhookEvents.All.Order(StringComparer.Ordinal))}.";

    public static bool BeAPublishedEvent(string @event)
    {
        return WebhookEvents.All.Contains(@event);
    }

    public static bool BeASubscribableEvent(string @event)
    {
        return WebhookEvents.Subscribable.Contains(@event);
    }
}
