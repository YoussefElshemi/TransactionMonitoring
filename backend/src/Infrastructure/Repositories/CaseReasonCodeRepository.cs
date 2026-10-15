using Core.Enums;
using Core.Interfaces.Repositories;
using Core.Models.CaseReasonCodes;
using Core.Models.Common;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class CaseReasonCodeRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider)
    : PagedRepository<ListCaseReasonCodeRequest, CaseReasonCodeEntity, CaseReasonCode>, ICaseReasonCodeRepository
{
    private const string LikeEscapeCharacter = "\\";

    public async Task AddAsync(CaseReasonCode caseReasonCode, CancellationToken stoppingToken)
    {
        var caseReasonCodeEntity = caseReasonCode.ToEntity();
        await context.CaseReasonCodes.AddAsync(caseReasonCodeEntity, stoppingToken);
    }

    public async Task<CaseReasonCode> GetAsync(CaseReasonCodeId caseReasonCodeId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        var caseReasonCodeEntity = await context.CaseReasonCodes
            .FirstAsync(x => x.CaseReasonCodeId == caseReasonCodeId && (includeDeleted || !x.IsDeleted), stoppingToken);

        return caseReasonCodeEntity.ToModel();
    }

    public async Task<CaseReasonCode?> FindByCodeAsync(OrganizationId organizationId, CaseReasonCodeName code, CancellationToken stoppingToken)
    {
        var value = EscapeLikePattern(code.ToString());

        var caseReasonCodeEntity = await context.CaseReasonCodes
            .FirstOrDefaultAsync(x => x.OrganizationId == organizationId &&
                                      EF.Functions.ILike(x.Code, value, LikeEscapeCharacter) &&
                                      !x.IsDeleted, stoppingToken);

        return caseReasonCodeEntity?.ToModel();
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, CaseReasonCodeId caseReasonCodeId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        var exists = await context.CaseReasonCodes
            .AnyAsync(x => x.OrganizationId == organizationId &&
                           x.CaseReasonCodeId == caseReasonCodeId &&
                           (includeDeleted || x.IsDeleted == false), stoppingToken);

        return new Exists(exists);
    }

    public async Task<Exists> AnyEnabledAsync(OrganizationId organizationId, CaseDisposition? disposition, CancellationToken stoppingToken)
    {
        var queryable = context.CaseReasonCodes
            .Where(x => x.OrganizationId == organizationId && x.IsEnabled && !x.IsDeleted);

        if (disposition.HasValue)
        {
            var dispositionId = (int)disposition.Value;

            queryable = queryable
                .Where(x => x.DispositionId == null || x.DispositionId == dispositionId);
        }

        var exists = await queryable.AnyAsync(stoppingToken);

        return new Exists(exists);
    }

    public async Task<CaseReasonCodeUsage> GetUsageAsync(CaseReasonCodeId caseReasonCodeId, CancellationToken stoppingToken)
    {
        var caseReasonCodeEntity = await context.CaseReasonCodes
            .FirstAsync(x => x.CaseReasonCodeId == caseReasonCodeId, stoppingToken);

        var caseCount = await context.Cases
            .CountAsync(x => x.CaseReasonCodeId == caseReasonCodeId && !x.IsDeleted, stoppingToken);

        return new CaseReasonCodeUsage
        {
            CaseReasonCodeId = new CaseReasonCodeId(caseReasonCodeEntity.CaseReasonCodeId),
            Code = new CaseReasonCodeName(caseReasonCodeEntity.Code),
            CaseCount = new TotalCount(caseCount)
        };
    }

    public async Task UpdateAsync(UpdateCaseReasonCodeRequest updateCaseReasonCodeRequest, CancellationToken stoppingToken)
    {
        var caseReasonCodeEntity = await context.CaseReasonCodes
            .FirstAsync(x => x.CaseReasonCodeId == updateCaseReasonCodeRequest.CaseReasonCodeId && !x.IsDeleted, stoppingToken);

        caseReasonCodeEntity.Description = updateCaseReasonCodeRequest.Description ?? caseReasonCodeEntity.Description;

        if (updateCaseReasonCodeRequest.ClearDisposition)
        {
            caseReasonCodeEntity.DispositionId = null;
        }
        else if (updateCaseReasonCodeRequest.Disposition.HasValue)
        {
            caseReasonCodeEntity.DispositionId = (int)updateCaseReasonCodeRequest.Disposition.Value;
        }

        caseReasonCodeEntity.IsEnabled = updateCaseReasonCodeRequest.IsEnabled ?? caseReasonCodeEntity.IsEnabled;
        caseReasonCodeEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.CaseReasonCodes.Update(caseReasonCodeEntity);
    }

    public async Task DeleteAsync(CaseReasonCodeId caseReasonCodeId, CancellationToken stoppingToken)
    {
        var caseReasonCodeEntity = await context.CaseReasonCodes
            .FirstAsync(x => x.CaseReasonCodeId == caseReasonCodeId, stoppingToken);

        caseReasonCodeEntity.IsDeleted = true;
        caseReasonCodeEntity.DeletedAt = timeProvider.GetUtcNow();

        context.CaseReasonCodes.Update(caseReasonCodeEntity);
    }

    public async Task RecoverAsync(CaseReasonCodeId caseReasonCodeId, CancellationToken stoppingToken)
    {
        var caseReasonCodeEntity = await context.CaseReasonCodes
            .FirstAsync(x => x.CaseReasonCodeId == caseReasonCodeId, stoppingToken);

        caseReasonCodeEntity.IsDeleted = false;
        caseReasonCodeEntity.DeletedAt = null;

        context.CaseReasonCodes.Update(caseReasonCodeEntity);
    }

    protected override Task<List<CaseReasonCode>> MapPageAsync(List<CaseReasonCodeEntity> items, ListCaseReasonCodeRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<CaseReasonCodeEntity> OrderByExpression()
    {
        return new OrderByExpression<CaseReasonCodeEntity, DateTimeOffset>(x => x.UpdatedAt);
    }

    protected override IQueryable<CaseReasonCodeEntity> FilterByPagedRequest(ListCaseReasonCodeRequest listCaseReasonCodeRequest)
    {
        var queryable = context.CaseReasonCodes
            .Where(x => x.OrganizationId == listCaseReasonCodeRequest.OrganizationId && !x.IsDeleted);

        if (listCaseReasonCodeRequest.Code.HasValue)
        {
            var term = EscapeLikePattern(listCaseReasonCodeRequest.Code.Value.ToString());

            queryable = queryable
                .Where(x => EF.Functions.ILike(x.Code, $"%{term}%", LikeEscapeCharacter));
        }

        if (listCaseReasonCodeRequest.Disposition.HasValue)
        {
            queryable = queryable
                .Where(x => x.DispositionId == (int)listCaseReasonCodeRequest.Disposition.Value);
        }

        if (listCaseReasonCodeRequest.IsEnabled.HasValue)
        {
            queryable = queryable
                .Where(x => x.IsEnabled == listCaseReasonCodeRequest.IsEnabled.Value);
        }

        return queryable;
    }

    private static string EscapeLikePattern(string value)
    {
        return value
            .Replace(LikeEscapeCharacter, LikeEscapeCharacter + LikeEscapeCharacter, StringComparison.Ordinal)
            .Replace("%", LikeEscapeCharacter + "%", StringComparison.Ordinal)
            .Replace("_", LikeEscapeCharacter + "_", StringComparison.Ordinal);
    }

    protected override IQueryable<CaseReasonCodeEntity> FilterByDateRange(IQueryable<CaseReasonCodeEntity> queryable, DatedRequest datedRequest)
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
