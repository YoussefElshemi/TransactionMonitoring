using Core.Enums;
using Core.Interfaces.Repositories;
using Core.Models.BackTests;
using Core.Models.Common;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class BackTestRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider) : PagedRepository<ListBackTestRequest, BackTestEntity, BackTest>, IBackTestRepository
{
    public async Task AddAsync(BackTest backTest, CancellationToken stoppingToken)
    {
        await context.BackTests.AddAsync(backTest.ToEntity(), stoppingToken);
    }

    public async Task<BackTest> GetAsync(BackTestId backTestId, CancellationToken stoppingToken)
    {
        var backTestEntity = await context.BackTests
            .FirstAsync(x => x.BackTestId == backTestId, stoppingToken);

        return backTestEntity.ToModel();
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, BackTestId backTestId, CancellationToken stoppingToken)
    {
        var exists = await context.BackTests
            .AnyAsync(x => x.BackTestId == backTestId &&
                           x.OrganizationId == organizationId, stoppingToken);

        return new Exists(exists);
    }

    public async Task<Exists> ExistsActiveAsync(OrganizationId organizationId, RuleId ruleId, CancellationToken stoppingToken)
    {
        var exists = await context.BackTests
            .AnyAsync(x => x.OrganizationId == organizationId &&
                           x.RuleId == ruleId &&
                           (x.StatusId == (int)BackTestStatus.Pending ||
                            x.StatusId == (int)BackTestStatus.Running), stoppingToken);

        return new Exists(exists);
    }

    public async Task UpdateAsync(UpdateBackTestRequest request, CancellationToken stoppingToken)
    {
        var backTestEntity = await context.BackTests.FindAsync([(Guid)request.BackTestId], stoppingToken);

        backTestEntity!.StatusId = (int)request.Status;
        backTestEntity.TotalEvaluations = request.TotalEvaluations;
        backTestEntity.ProcessedCount = request.ProcessedCount;
        backTestEntity.CompletedAt = request.CompletedAt;
        backTestEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.BackTests.Update(backTestEntity);
    }

    protected override Task<List<BackTest>> MapPageAsync(List<BackTestEntity> items, ListBackTestRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<BackTestEntity> OrderByExpression()
    {
        return new OrderByExpression<BackTestEntity, DateTimeOffset>(x => x.UpdatedAt);
    }

    protected override IQueryable<BackTestEntity> FilterByPagedRequest(ListBackTestRequest request)
    {
        var queryable = context.BackTests
            .Where(x => x.OrganizationId == request.OrganizationId);

        if (request.RuleId.HasValue)
        {
            queryable = queryable.Where(x => x.RuleId == request.RuleId.Value);
        }

        return queryable;
    }

    protected override IQueryable<BackTestEntity> FilterByDateRange(IQueryable<BackTestEntity> queryable, DatedRequest datedRequest)
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
