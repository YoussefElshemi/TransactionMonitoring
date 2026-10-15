using Core.Configs;
using Core.Constants;
using Core.Interfaces.Services;
using Core.Models.Webhooks;
using Core.ValueObjects;
using Microsoft.Extensions.Options;

namespace Core.Services;

public class WebhookCatalogueService(IOptions<AppConfig> appConfig) : IWebhookCatalogueService
{
    public WebhookCatalogue Get()
    {
        return new WebhookCatalogue
        {
            Events = WebhookEventCatalogue.Events,
            CurrentPayloadVersion = new WebhookPayloadVersion(WebhookPayloadVersions.Current),
            SupportedPayloadVersions = WebhookPayloadVersions.Supported
                .Order()
                .Select(x => new WebhookPayloadVersion(x))
                .ToList(),
            SourceIpRanges = appConfig.Value.WebhookConfig.SourceIpRanges
                .Select(x => new SourceIpRange(x))
                .ToList()
        };
    }
}
