using Core.Interfaces.Repositories;
using Core.Models.CaseNotes;
using Core.Models.Common;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class CaseNoteRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider) : PagedRepository<ListCaseNoteRequest, CaseNoteEntity, CaseNote>, ICaseNoteRepository
{
    public async Task AddAsync(CaseNote caseNote, CancellationToken stoppingToken)
    {
        var caseNoteEntity = caseNote.ToEntity();
        await context.CaseNotes.AddAsync(caseNoteEntity, stoppingToken);
    }

    public async Task UpdateAsync(UpdateCaseNoteRequest updateCaseNoteRequest, CancellationToken stoppingToken)
    {
        var caseNoteEntity = await context.CaseNotes
            .FirstAsync(x => x.CaseNoteId == updateCaseNoteRequest.CaseNoteId && !x.IsDeleted, stoppingToken);

        caseNoteEntity.Message = updateCaseNoteRequest.Message ?? caseNoteEntity.Message;
        caseNoteEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.CaseNotes.Update(caseNoteEntity);
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, CaseId caseId, CaseNoteId caseNoteId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        var exists = await context.CaseNotes
            .AnyAsync(x => x.CaseNoteId == caseNoteId &&
                           x.CaseId == caseId &&
                           x.OrganizationId == organizationId &&
                           (includeDeleted || x.IsDeleted == false), stoppingToken);

        return new Exists(exists);
    }

    public async Task<CaseNote> GetAsync(CaseNoteId caseNoteId, CancellationToken stoppingToken)
    {
        var caseNoteEntity = await context.CaseNotes
            .FirstAsync(x => x.CaseNoteId == caseNoteId && !x.IsDeleted, stoppingToken);

        return caseNoteEntity.ToModel();
    }

    public async Task DeleteAsync(CaseNoteId caseNoteId, CancellationToken stoppingToken)
    {
        var caseNoteEntity = await context.CaseNotes
            .FirstAsync(x => x.CaseNoteId == caseNoteId, stoppingToken);

        caseNoteEntity.IsDeleted = true;
        caseNoteEntity.DeletedAt = timeProvider.GetUtcNow();

        context.CaseNotes.Update(caseNoteEntity);
    }

    public async Task RecoverAsync(CaseNoteId caseNoteId, CancellationToken stoppingToken)
    {
        var caseNoteEntity = await context.CaseNotes
            .FirstAsync(x => x.CaseNoteId == caseNoteId, stoppingToken);

        caseNoteEntity.IsDeleted = false;
        caseNoteEntity.DeletedAt = null;

        context.CaseNotes.Update(caseNoteEntity);
    }

    protected override Task<List<CaseNote>> MapPageAsync(List<CaseNoteEntity> items, ListCaseNoteRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<CaseNoteEntity> OrderByExpression()
    {
        return new OrderByExpression<CaseNoteEntity, DateTimeOffset>(x => x.UpdatedAt);
    }

    protected override IQueryable<CaseNoteEntity> FilterByPagedRequest(ListCaseNoteRequest listCaseNoteRequest)
    {
        var queryable = context.CaseNotes
            .Where(x => x.CaseId == listCaseNoteRequest.CaseId && !x.IsDeleted);

        if (listCaseNoteRequest.Message.HasValue)
        {
            queryable = queryable
                .Where(x => EF.Functions.ILike(x.Message, $"%{listCaseNoteRequest.Message}%"));
        }

        return queryable;
    }

    protected override IQueryable<CaseNoteEntity> FilterByDateRange(IQueryable<CaseNoteEntity> queryable, DatedRequest datedRequest)
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