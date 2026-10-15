using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.Invoices;
using Core.ValueObjects;
using Presentation.Mappers.Invoices;
using Presentation.Mappers.Payments;
using Presentation.Models.Invoices;

namespace Presentation.Factories;

public class InvoiceResponseFactory(RelatedRecordLoader relatedRecordLoader, IPaymentService paymentService)
{
    public async Task<InvoiceResourceDto> CreateAsync(Invoice invoice, EnrichmentContext context, CancellationToken stoppingToken)
    {
        var resources = await CreateAsync([invoice], context, stoppingToken);

        return resources[0];
    }

    public async Task<InvoiceResourceDto[]> CreateAsync(
        IReadOnlyCollection<Invoice> invoices, EnrichmentContext context, CancellationToken stoppingToken)
    {
        if (invoices.Count == 0)
        {
            return [];
        }

        var organizations = await relatedRecordLoader.LoadOrganizationsAsync(
            invoices.Select(x => (OrganizationId?)x.OrganizationId), context, stoppingToken);
        var createdByUsers = await relatedRecordLoader.LoadUsersAsync(
            invoices.Select(x => x.CreatedByUserId), context, stoppingToken);

        var settlements = await paymentService.GetSettlementsAsync(
            invoices.Where(x => x.InvoiceStatus is InvoiceStatus.Issued).Select(x => x.InvoiceId).ToList(),
            stoppingToken);

        var settlementsByInvoice = settlements.ToDictionary(x => (Guid)x.InvoiceId);

        return invoices.Select(x => new InvoiceResourceDto
        {
            Invoice = x.ToDto(),
            Organization = organizations.GetValueOrDefault(x.OrganizationId),
            CreatedByUser = x.CreatedByUserId.HasValue ? createdByUsers.GetValueOrDefault(x.CreatedByUserId.Value) : null,
            Settlement = settlementsByInvoice.TryGetValue(x.InvoiceId, out var settlement) ? settlement.ToDto() : null
        }).ToArray();
    }
}
