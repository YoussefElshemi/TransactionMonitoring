using Core.Models.Webhooks;

namespace Core.Interfaces.Services;

public interface IWebhookCatalogueService
{
    WebhookCatalogue Get();
}
