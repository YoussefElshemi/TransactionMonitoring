using Core.Models.WebhookConfigurations;
using Core.ValueObjects;
using Presentation.Models.WebhookConfigurations;

namespace Presentation.Mappers.OrganizationWebhookConfigurations;

public static class ListMyOrganizationWebhookConfigurationRequestMapper
{
    public static ListOrganizationWebhookConfigurationRequest ToModel(this ListMyOrganizationWebhookConfigurationRequestDto listMyOrganizationWebhookConfigurationRequestDto)
    {
        return new ListOrganizationWebhookConfigurationRequest
        {
            OrganizationId = new OrganizationId(listMyOrganizationWebhookConfigurationRequestDto.OrganizationId),
            FriendlyName = !string.IsNullOrEmpty(listMyOrganizationWebhookConfigurationRequestDto.FriendlyName) ? new FriendlyName(listMyOrganizationWebhookConfigurationRequestDto.FriendlyName) : null,
            PageNumber = new PageNumber(listMyOrganizationWebhookConfigurationRequestDto.PageNumber),
            PageSize = new PageSize(listMyOrganizationWebhookConfigurationRequestDto.PageSize),
            FromDate = new DateRange(listMyOrganizationWebhookConfigurationRequestDto.FromDate),
            ToDate = new DateRange(listMyOrganizationWebhookConfigurationRequestDto.ToDate)
        };
    }
}
