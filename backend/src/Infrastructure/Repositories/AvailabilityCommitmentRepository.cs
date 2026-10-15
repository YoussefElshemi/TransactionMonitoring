using Core.Interfaces.Repositories;
using Core.Models.Availability;
using Core.Models.Common;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class AvailabilityCommitmentRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider) : PagedRepository<ListAvailabilityCommitmentRequest, AvailabilityCommitmentEntity, AvailabilityCommitment>, IAvailabilityCommitmentRepository
{
    public async Task AddAsync(AvailabilityCommitment availabilityCommitment, CancellationToken stoppingToken)
    {
        var availabilityCommitmentEntity = availabilityCommitment.ToEntity();
        await context.AvailabilityCommitments.AddAsync(availabilityCommitmentEntity, stoppingToken);
    }

    public async Task<AvailabilityCommitment> GetAsync(AvailabilityCommitmentId availabilityCommitmentId, CancellationToken stoppingToken)
    {
        var availabilityCommitmentEntity = await Queryable()
            .FirstAsync(x => x.AvailabilityCommitmentId == availabilityCommitmentId && !x.IsDeleted, stoppingToken);

        return availabilityCommitmentEntity.ToModel();
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, AvailabilityCommitmentId availabilityCommitmentId, CancellationToken stoppingToken)
    {
        var exists = await context.AvailabilityCommitments
            .AnyAsync(x => x.OrganizationId == organizationId &&
                           x.AvailabilityCommitmentId == availabilityCommitmentId &&
                           !x.IsDeleted, stoppingToken);

        return new Exists(exists);
    }

    public async Task<AvailabilityCommitment?> GetInEffectAsync(OrganizationId organizationId, DateTimeOffset asOf, CancellationToken stoppingToken)
    {
        var availabilityCommitmentEntity = await Queryable()
            .Where(x => x.OrganizationId == organizationId &&
                        !x.IsDeleted &&
                        x.EffectiveFrom <= asOf &&
                        (x.EffectiveTo == null || x.EffectiveTo > asOf))
            .OrderByDescending(x => x.EffectiveFrom)
            .FirstOrDefaultAsync(stoppingToken);

        return availabilityCommitmentEntity?.ToModel();
    }

    public async Task<List<AvailabilityCommitment>> ListInEffectAsync(DateTimeOffset asOf, CancellationToken stoppingToken)
    {
        var availabilityCommitmentEntities = await Queryable()
            .Where(x => !x.IsDeleted &&
                        x.EffectiveFrom <= asOf &&
                        (x.EffectiveTo == null || x.EffectiveTo > asOf))
            .OrderBy(x => x.OrganizationId)
            .ToListAsync(stoppingToken);

        return availabilityCommitmentEntities.ConvertAll(x => x.ToModel());
    }

    public async Task CloseAsync(AvailabilityCommitmentId availabilityCommitmentId, EffectiveTo effectiveTo, CancellationToken stoppingToken)
    {
        var availabilityCommitmentEntity = await context.AvailabilityCommitments
            .FirstAsync(x => x.AvailabilityCommitmentId == availabilityCommitmentId && !x.IsDeleted, stoppingToken);

        availabilityCommitmentEntity.EffectiveTo = effectiveTo;
        availabilityCommitmentEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.AvailabilityCommitments.Update(availabilityCommitmentEntity);
    }

    public async Task DeleteAsync(AvailabilityCommitmentId availabilityCommitmentId, CancellationToken stoppingToken)
    {
        var availabilityCommitmentEntity = await context.AvailabilityCommitments
            .FirstAsync(x => x.AvailabilityCommitmentId == availabilityCommitmentId && !x.IsDeleted, stoppingToken);

        var now = timeProvider.GetUtcNow();

        availabilityCommitmentEntity.IsDeleted = true;
        availabilityCommitmentEntity.DeletedAt = now;
        availabilityCommitmentEntity.UpdatedAt = now;

        context.AvailabilityCommitments.Update(availabilityCommitmentEntity);
    }

    protected override Task<List<AvailabilityCommitment>> MapPageAsync(List<AvailabilityCommitmentEntity> items, ListAvailabilityCommitmentRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<AvailabilityCommitmentEntity> OrderByExpression()
    {
        return new OrderByExpression<AvailabilityCommitmentEntity, DateTimeOffset>(x => x.EffectiveFrom);
    }

    protected override IQueryable<AvailabilityCommitmentEntity> FilterByPagedRequest(ListAvailabilityCommitmentRequest listAvailabilityCommitmentRequest)
    {
        var queryable = Queryable()
            .Where(x => x.OrganizationId == listAvailabilityCommitmentRequest.OrganizationId && !x.IsDeleted);

        if (!listAvailabilityCommitmentRequest.IncludeSuperseded)
        {
            queryable = queryable.Where(x => x.EffectiveTo == null);
        }

        return queryable;
    }

    protected override IQueryable<AvailabilityCommitmentEntity> FilterByDateRange(IQueryable<AvailabilityCommitmentEntity> queryable, DatedRequest datedRequest)
    {
        if (datedRequest.FromDate != DateOnly.MinValue)
        {
            queryable = queryable
                .Where(x => DateOnly.FromDateTime(x.EffectiveFrom.UtcDateTime) >= datedRequest.FromDate);
        }

        if (datedRequest.ToDate != DateOnly.MaxValue)
        {
            queryable = queryable
                .Where(x => DateOnly.FromDateTime(x.EffectiveFrom.UtcDateTime) <= datedRequest.ToDate);
        }

        return queryable;
    }

    private IQueryable<AvailabilityCommitmentEntity> Queryable()
    {
        return context.AvailabilityCommitments.Include(x => x.CreditBands);
    }
}
