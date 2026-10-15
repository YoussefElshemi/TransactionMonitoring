using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IDocumentNumberRepository
{
    Task<int> NextInvoicePositionAsync(BillingEntityId billingEntityId, Year year, CancellationToken stoppingToken);
    Task<int> NextCreditNotePositionAsync(BillingEntityId billingEntityId, Year year, CancellationToken stoppingToken);

    Task<int> NextReceiptPositionAsync(BillingEntityId billingEntityId, Year year, CancellationToken stoppingToken);
}
