using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Mappers.OrganizationWebhookConfigurations;
using Presentation.Models.WebhookConfigurations;
using Presentation.Validators.Common;

namespace Presentation.Validators.OrganizationWebhooks;

public class ListOrganizationWebhookConfigurationRequestValidator(
    IWebhookConfigurationService webhookConfigurationService,
    IOrganizationService organizationService,
    TimeProvider timeProvider)
    : PagedRequestValidator<ListOrganizationWebhookConfigurationRequestDto>(timeProvider)
{
    protected sealed override void Initialize()
    {
        base.Initialize();

        RuleFor(x => x.OrganizationId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await organizationService.ExistsAsync(new OrganizationId(x.OrganizationId), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Organization not found");
    }

    protected override Task<TotalCount> GetCountAsync(ListOrganizationWebhookConfigurationRequestDto request, CancellationToken stoppingToken)
    {
        return webhookConfigurationService.GetCountAsync(request.ToModel(), stoppingToken);
    }
}