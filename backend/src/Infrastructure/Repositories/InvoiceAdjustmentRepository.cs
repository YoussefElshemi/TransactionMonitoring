using Core.Exceptions;
using Core.Interfaces.Repositories;
using Core.Models.Adjustments;
using Core.Models.Common;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class InvoiceAdjustmentRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider) : PagedRepository<ListInvoiceAdjustmentRequest, InvoiceAdjustmentEntity, InvoiceAdjustment>, IInvoiceAdjustmentRepository
{
    public async Task AddAsync(InvoiceAdjustment invoiceAdjustment, CancellationToken stoppingToken)
    {
        var invoiceAdjustmentEntity = invoiceAdjustment.ToEntity();
        await context.InvoiceAdjustments.AddAsync(invoiceAdjustmentEntity, stoppingToken);
    }

    public async Task<InvoiceAdjustment> GetAsync(InvoiceAdjustmentId invoiceAdjustmentId, CancellationToken stoppingToken)
    {
        var invoiceAdjustmentEntity = await context.InvoiceAdjustments
            .FirstAsync(x => x.InvoiceAdjustmentId == invoiceAdjustmentId && !x.IsDeleted, stoppingToken);

        return invoiceAdjustmentEntity.ToModel();
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, InvoiceAdjustmentId invoiceAdjustmentId, CancellationToken stoppingToken)
    {
        var exists = await context.InvoiceAdjustments
            .AnyAsync(x => x.OrganizationId == organizationId &&
                           x.InvoiceAdjustmentId == invoiceAdjustmentId &&
                           !x.IsDeleted, stoppingToken);

        return new Exists(exists);
    }

    public async Task<List<InvoiceAdjustment>> GetUnappliedAsync(OrganizationId organizationId, Month month, Year year, CancellationToken stoppingToken)
    {
        var invoiceAdjustmentEntities = await context.InvoiceAdjustments
            .Where(x => x.OrganizationId == organizationId &&
                        x.Month == month &&
                        x.Year == year &&
                        x.AppliedToInvoiceId == null &&
                        !x.IsDeleted)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(stoppingToken);

        return invoiceAdjustmentEntities.ConvertAll(x => x.ToModel());
    }

    public async Task ApplyAsync(InvoiceAdjustmentId invoiceAdjustmentId, InvoiceId invoiceId, CancellationToken stoppingToken)
    {
        var invoiceAdjustmentEntity = await context.InvoiceAdjustments
            .FirstAsync(x => x.InvoiceAdjustmentId == invoiceAdjustmentId && !x.IsDeleted, stoppingToken);

        invoiceAdjustmentEntity.AppliedToInvoiceId = invoiceId;
        invoiceAdjustmentEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.InvoiceAdjustments.Update(invoiceAdjustmentEntity);
    }

    public async Task ReleaseAsync(InvoiceId invoiceId, CancellationToken stoppingToken)
    {
        var invoiceAdjustmentEntities = await context.InvoiceAdjustments
            .Where(x => x.AppliedToInvoiceId == invoiceId)
            .ToListAsync(stoppingToken);

        foreach (var invoiceAdjustmentEntity in invoiceAdjustmentEntities)
        {
            invoiceAdjustmentEntity.AppliedToInvoiceId = null;
            invoiceAdjustmentEntity.UpdatedAt = timeProvider.GetUtcNow();

            context.InvoiceAdjustments.Update(invoiceAdjustmentEntity);
        }
    }

    public async Task ReapplyAsync(InvoiceId invoiceId, CancellationToken stoppingToken)
    {
        var ids = await context.InvoiceLines
            .Where(x => x.InvoiceId == invoiceId && x.InvoiceAdjustmentId != null)
            .Select(x => x.InvoiceAdjustmentId!.Value)
            .ToListAsync(stoppingToken);

        var invoiceAdjustmentEntities = await context.InvoiceAdjustments
            .Where(x => ids.Contains(x.InvoiceAdjustmentId) && !x.IsDeleted)
            .ToListAsync(stoppingToken);

        foreach (var invoiceAdjustmentEntity in invoiceAdjustmentEntities)
        {
            if (invoiceAdjustmentEntity.AppliedToInvoiceId is not null &&
                invoiceAdjustmentEntity.AppliedToInvoiceId != invoiceId)
            {
                throw new InvoiceAdjustmentAppliedException(new InvoiceId(invoiceAdjustmentEntity.AppliedToInvoiceId.Value));
            }

            invoiceAdjustmentEntity.AppliedToInvoiceId = invoiceId;
            invoiceAdjustmentEntity.UpdatedAt = timeProvider.GetUtcNow();

            context.InvoiceAdjustments.Update(invoiceAdjustmentEntity);
        }
    }

    public async Task DeleteAsync(InvoiceAdjustmentId invoiceAdjustmentId, CancellationToken stoppingToken)
    {
        var invoiceAdjustmentEntity = await context.InvoiceAdjustments
            .FirstAsync(x => x.InvoiceAdjustmentId == invoiceAdjustmentId && !x.IsDeleted, stoppingToken);

        invoiceAdjustmentEntity.IsDeleted = true;
        invoiceAdjustmentEntity.DeletedAt = timeProvider.GetUtcNow();
        invoiceAdjustmentEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.InvoiceAdjustments.Update(invoiceAdjustmentEntity);
    }

    protected override Task<List<InvoiceAdjustment>> MapPageAsync(List<InvoiceAdjustmentEntity> items, ListInvoiceAdjustmentRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<InvoiceAdjustmentEntity> OrderByExpression()
    {
        return new OrderByExpression<InvoiceAdjustmentEntity, DateTimeOffset>(x => x.CreatedAt);
    }

    protected override IQueryable<InvoiceAdjustmentEntity> FilterByPagedRequest(ListInvoiceAdjustmentRequest listInvoiceAdjustmentRequest)
    {
        var queryable = context.InvoiceAdjustments
            .Where(x => x.OrganizationId == listInvoiceAdjustmentRequest.OrganizationId && !x.IsDeleted);

        if (!listInvoiceAdjustmentRequest.IncludeApplied)
        {
            queryable = queryable.Where(x => x.AppliedToInvoiceId == null);
        }

        return queryable;
    }

    protected override IQueryable<InvoiceAdjustmentEntity> FilterByDateRange(IQueryable<InvoiceAdjustmentEntity> queryable, DatedRequest datedRequest)
    {
        if (datedRequest.FromDate != DateOnly.MinValue)
        {
            queryable = queryable
                .Where(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) >= datedRequest.FromDate);
        }

        if (datedRequest.ToDate != DateOnly.MaxValue)
        {
            queryable = queryable
                .Where(x => DateOnly.FromDateTime(x.CreatedAt.UtcDateTime) <= datedRequest.ToDate);
        }

        return queryable;
    }
}
