using Core.Exceptions;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Models.Adjustments;
using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Services;

public class InvoiceAdjustmentService(
    IInvoiceAdjustmentRepository invoiceAdjustmentRepository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : IInvoiceAdjustmentService
{
    public async Task<InvoiceAdjustment> CreateAsync(CreateInvoiceAdjustmentRequest createInvoiceAdjustmentRequest, CancellationToken stoppingToken)
    {
        var invoiceAdjustment = createInvoiceAdjustmentRequest.ToModel(timeProvider);

        if (invoiceAdjustment.Amount == 0)
        {
            throw new InvalidPriceStructureException("an adjustment of nothing is not an adjustment.");
        }

        await invoiceAdjustmentRepository.AddAsync(invoiceAdjustment, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await GetAsync(invoiceAdjustment.InvoiceAdjustmentId, stoppingToken);
    }

    public Task<InvoiceAdjustment> GetAsync(InvoiceAdjustmentId invoiceAdjustmentId, CancellationToken stoppingToken)
    {
        return invoiceAdjustmentRepository.GetAsync(invoiceAdjustmentId, stoppingToken);
    }

    public Task<Exists> ExistsAsync(OrganizationId organizationId, InvoiceAdjustmentId invoiceAdjustmentId, CancellationToken stoppingToken)
    {
        return invoiceAdjustmentRepository.ExistsAsync(organizationId, invoiceAdjustmentId, stoppingToken);
    }

    public Task<List<InvoiceAdjustment>> GetUnappliedAsync(OrganizationId organizationId, Month month, Year year, CancellationToken stoppingToken)
    {
        return invoiceAdjustmentRepository.GetUnappliedAsync(organizationId, month, year, stoppingToken);
    }

    public Task<PagedResponse<InvoiceAdjustment>> ListAsync(ListInvoiceAdjustmentRequest listInvoiceAdjustmentRequest, CancellationToken stoppingToken)
    {
        return invoiceAdjustmentRepository.ListAsync(listInvoiceAdjustmentRequest, stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListInvoiceAdjustmentRequest listInvoiceAdjustmentRequest, CancellationToken stoppingToken)
    {
        return invoiceAdjustmentRepository.GetCountAsync(listInvoiceAdjustmentRequest, stoppingToken);
    }

    public async Task DeleteAsync(InvoiceAdjustmentId invoiceAdjustmentId, CancellationToken stoppingToken)
    {
        var invoiceAdjustment = await GetAsync(invoiceAdjustmentId, stoppingToken);

        if (invoiceAdjustment.AppliedToInvoiceId is not null)
        {
            throw new InvoiceAdjustmentAppliedException(invoiceAdjustment.AppliedToInvoiceId.Value);
        }

        await invoiceAdjustmentRepository.DeleteAsync(invoiceAdjustmentId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }
}
