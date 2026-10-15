using Core.Models.WebhookConfigurations;
using Core.ValueObjects;
using Presentation.Models.WebhookConfigurations;

namespace Presentation.Mappers.OrganizationWebhookConfigurations;

public static class ListOrganizationWebhookConfigurationRequestMapper
{
    public static ListOrganizationWebhookConfigurationRequest ToModel(this ListOrganizationWebhookConfigurationRequestDto listOrganizationWebhookConfigurationRequestDto)
    {
        return new ListOrganizationWebhookConfigurationRequest
        {
            OrganizationId = new OrganizationId(listOrganizationWebhookConfigurationRequestDto.OrganizationId),
            FriendlyName = !string.IsNullOrEmpty(listOrganizationWebhookConfigurationRequestDto.FriendlyName) ? new FriendlyName(listOrganizationWebhookConfigurationRequestDto.FriendlyName) : null,
            PageNumber = new PageNumber(listOrganizationWebhookConfigurationRequestDto.PageNumber),
            PageSize = new PageSize(listOrganizationWebhookConfigurationRequestDto.PageSize),
            FromDate = new DateRange(listOrganizationWebhookConfigurationRequestDto.FromDate),
            ToDate = new DateRange(listOrganizationWebhookConfigurationRequestDto.ToDate)
        };
    }
}