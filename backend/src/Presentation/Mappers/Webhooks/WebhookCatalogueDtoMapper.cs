using Core.Models.Webhooks;
using Presentation.Models.Webhooks;

namespace Presentation.Mappers.Webhooks;

public static class WebhookCatalogueDtoMapper
{
    public static WebhookCatalogueResponseDto ToDto(this WebhookCatalogue webhookCatalogue)
    {
        return new WebhookCatalogueResponseDto
        {
            Events = webhookCatalogue.Events.Select(x => new WebhookEventDefinitionResponseDto
            {
                Event = x.Event.ToString(),
                Category = x.Category.ToString(),
                Description = x.Description.ToString(),
                IsSubscribable = x.IsSubscribable,
                IntroducedInVersion = x.IntroducedInVersion
            }).ToArray(),
            CurrentPayloadVersion = webhookCatalogue.CurrentPayloadVersion,
            SupportedPayloadVersions = webhookCatalogue.SupportedPayloadVersions.Select(x => (int)x).ToArray(),
            SourceIpRanges = webhookCatalogue.SourceIpRanges.Select(x => x.ToString()).ToArray()
        };
    }
}
