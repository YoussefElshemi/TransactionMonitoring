using Core.Helpers;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.BackTests;
using Core.Models.Common;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class BackTestResultRepository(
    TransactionMonitoringDbContext context,
    IPayloadProtectionService payloadProtectionService) : PagedRepository<ListBackTestResultRequest, BackTestResultEntity, BackTestResult>, IBackTestResultRepository
{
    public async Task AddRangeAsync(IReadOnlyList<BackTestResult> results, CancellationToken stoppingToken)
    {
        await context.BackTestResults.AddRangeAsync(results.Select(x => x.ToEntity()), stoppingToken);
    }

    protected override async Task<List<BackTestResult>> MapPageAsync(List<BackTestResultEntity> items, ListBackTestResultRequest request, CancellationToken stoppingToken)
    {
        var ruleEvaluationIds = items.ConvertAll(x => x.RuleEvaluationId);

        var payloads = await context.RuleEvaluations
            .IgnoreAutoIncludes()
            .Where(x => ruleEvaluationIds.Contains(x.RuleEvaluationId))
            .Select(x => new { x.RuleEvaluationId, x.OrganizationId, x.Transaction.Payload })
            .ToDictionaryAsync(x => x.RuleEvaluationId, x => x, stoppingToken);

        var dataKeys = await payloadProtectionService.FindDataKeysAsync(
            payloads.Values.Select(x => new OrganizationId(x.OrganizationId)).ToList(), stoppingToken);

        return items.ConvertAll(x =>
        {
            var payload = payloads[x.RuleEvaluationId];
            var organizationId = new OrganizationId(payload.OrganizationId);

            return x.ToModel(PayloadProtector.Read(
                dataKeys.TryGetValue(organizationId, out var dataKey) ? dataKey : null, organizationId, payload.Payload));
        });
    }

    protected override IOrderByExpression<BackTestResultEntity> OrderByExpression()
    {
        return new OrderByExpression<BackTestResultEntity, DateTimeOffset>(x => x.CreatedAt);
    }

    protected override IQueryable<BackTestResultEntity> FilterByPagedRequest(ListBackTestResultRequest request)
    {
        var queryable = context.BackTestResults
            .Where(x => x.BackTestId == request.BackTestId);

        if (request.ChangedOnly == true)
        {
            queryable = queryable.Where(x => x.HasChanged);
        }

        return queryable;
    }

    protected override IQueryable<BackTestResultEntity> FilterByDateRange(IQueryable<BackTestResultEntity> queryable, DatedRequest datedRequest)
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
