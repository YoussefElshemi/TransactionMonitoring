using Core.Helpers;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.BackTests;
using Core.Models.Common;
using Core.Models.RuleEvaluations;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class RuleEvaluationRepository(
    TransactionMonitoringDbContext context,
    IPayloadProtectionService payloadProtectionService) : PagedRepository<ListRuleEvaluationRequest, RuleEvaluationEntity, RuleEvaluation>, IRuleEvaluationRepository
{
    public async Task AddAsync(RuleEvaluation ruleEvaluation, CancellationToken stoppingToken)
    {
        var ruleEvaluationEntity = ruleEvaluation.ToEntity();
        await context.RuleEvaluations.AddAsync(ruleEvaluationEntity, stoppingToken);
    }

    public async Task AddRangeAsync(List<RuleEvaluation> ruleEvaluations, CancellationToken stoppingToken)
    {
        var ruleEvaluationEntities = ruleEvaluations.Select(x => x.ToEntity());
        await context.RuleEvaluations.AddRangeAsync(ruleEvaluationEntities, stoppingToken);
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, RuleEvaluationId ruleEvaluationId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        var exists = await context.RuleEvaluations
            .AnyAsync(x => x.RuleEvaluationId == ruleEvaluationId &&
                           x.OrganizationId == organizationId &&
                           (includeDeleted || x.IsDeleted == false), stoppingToken);

        return new Exists(exists);
    }

    public async Task<RuleEvaluation> GetAsync(RuleEvaluationId ruleEvaluationId, CancellationToken stoppingToken)
    {
        var ruleEvaluationEntity = await context.RuleEvaluations
            .FirstAsync(x => x.RuleEvaluationId == ruleEvaluationId && !x.IsDeleted, stoppingToken);

        return ruleEvaluationEntity.ToModel();
    }

    protected override Task<List<RuleEvaluation>> MapPageAsync(List<RuleEvaluationEntity> items, ListRuleEvaluationRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<RuleEvaluationEntity> OrderByExpression()
    {
        return new OrderByExpression<RuleEvaluationEntity, DateTimeOffset>(x => x.UpdatedAt);
    }

    protected override IQueryable<RuleEvaluationEntity> FilterByPagedRequest(ListRuleEvaluationRequest listRuleEvaluationRequest)
    {
        var queryable = context.RuleEvaluations
            .Where(x => x.OrganizationId == listRuleEvaluationRequest.OrganizationId && !x.IsDeleted);

        if (listRuleEvaluationRequest.RuleId.HasValue)
        {
            queryable = queryable
                .Where(x => x.RuleId == listRuleEvaluationRequest.RuleId.Value);
        }

        return queryable;
    }

    protected override IQueryable<RuleEvaluationEntity> FilterByDateRange(IQueryable<RuleEvaluationEntity> queryable, DatedRequest datedRequest)
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

    public async Task<TotalCount> CountByRuleAsync(OrganizationId organizationId, RuleId ruleId, CancellationToken stoppingToken)
    {
        var count = await context.RuleEvaluations
            .CountAsync(x => x.OrganizationId == organizationId && x.RuleId == ruleId && !x.IsDeleted, stoppingToken);

        return new TotalCount(count);
    }

    public async Task<List<BackTestSample>> ListChunkForBackTestAsync(OrganizationId organizationId, RuleId ruleId, BackTestId backTestId, int chunkSize, CancellationToken stoppingToken)
    {
        var backTestIdGuid = (Guid)backTestId;
        var samples = await context.RuleEvaluations
            .IgnoreAutoIncludes()
            .Where(x => x.OrganizationId == organizationId && x.RuleId == ruleId && !x.IsDeleted)
            .Where(x => !context.BackTestResults.Any(br => br.RuleEvaluationId == x.RuleEvaluationId && br.BackTestId == backTestIdGuid))
            .OrderBy(x => x.RuleEvaluationId)
            .Take(chunkSize)
            .Select(x => new { x.RuleEvaluationId, x.TransactionId, x.Matched, x.Transaction.Payload, x.CreatedAt })
            .ToListAsync(stoppingToken);

        var dataKey = await payloadProtectionService.FindDataKeyAsync(organizationId, stoppingToken);

        return samples.ConvertAll(x => new BackTestSample
        {
            RuleEvaluationId = new RuleEvaluationId(x.RuleEvaluationId),
            TransactionId = new TransactionId(x.TransactionId),
            Payload = PayloadProtector.Read(dataKey, organizationId, x.Payload),
            Matched = new Matched(x.Matched),
            CreatedAt = new CreatedAt(x.CreatedAt)
        });
    }
}