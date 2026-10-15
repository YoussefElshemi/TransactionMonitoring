using Core.Enums;
using Core.Interfaces.Repositories;
using Core.Models.Approvals;
using Core.Models.Common;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ApprovalRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider) : PagedRepository<ListApprovalRequest, ApprovalEntity, Approval>, IApprovalRepository
{
    public async Task AddAsync(Approval approval, CancellationToken stoppingToken)
    {
        await context.Approvals.AddAsync(approval.ToEntity(), stoppingToken);
    }

    public async Task<Approval> GetAsync(ApprovalId approvalId, CancellationToken stoppingToken)
    {
        var approvalEntity = await WithUsers()
            .FirstAsync(x => x.ApprovalId == approvalId, stoppingToken);

        return approvalEntity.ToModel();
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, ApprovalId approvalId, CancellationToken stoppingToken)
    {
        var exists = await context.Approvals
            .AnyAsync(x => x.OrganizationId == organizationId && x.ApprovalId == approvalId, stoppingToken);

        return new Exists(exists);
    }

    public async Task<Approval?> FindPendingByCaseAsync(CaseId caseId, CancellationToken stoppingToken)
    {
        var approvalEntity = await WithUsers()
            .FirstOrDefaultAsync(x => x.CaseId == caseId && x.StatusId == (int)ApprovalStatus.Pending, stoppingToken);

        return approvalEntity?.ToModel();
    }

    public async Task<Approval?> FindPendingBySuspiciousActivityReportAsync(
        SuspiciousActivityReportId suspiciousActivityReportId,
        CancellationToken stoppingToken)
    {
        var approvalEntity = await WithUsers()
            .FirstOrDefaultAsync(
                x => x.SuspiciousActivityReportId == suspiciousActivityReportId && x.StatusId == (int)ApprovalStatus.Pending,
                stoppingToken);

        return approvalEntity?.ToModel();
    }

    // Loaded without users: Update attaches the whole graph, and a pinned user blocks the close that follows from attaching the assignee.
    public async Task<Approval> ReviewAsync(ReviewApprovalRequest reviewApprovalRequest, CancellationToken stoppingToken)
    {
        var approvalEntity = await context.Approvals
            .FirstAsync(x => x.ApprovalId == reviewApprovalRequest.ApprovalId, stoppingToken);

        approvalEntity.StatusId = (int)reviewApprovalRequest.Status;
        approvalEntity.ReviewedByUserId = reviewApprovalRequest.ReviewedByUserId;
        approvalEntity.ReviewedAt = timeProvider.GetUtcNow();
        approvalEntity.ReviewNote = reviewApprovalRequest.ReviewNote?.ToString();
        approvalEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.Approvals.Update(approvalEntity);

        var users = await context.Users
            .Where(x => x.Id == approvalEntity.RequestedByUserId || x.Id == approvalEntity.ReviewedByUserId)
            .ToListAsync(stoppingToken);

        return approvalEntity.ToModel() with
        {
            RequestedByUser = users.FirstOrDefault(x => x.Id == approvalEntity.RequestedByUserId)?.ToModel(),
            ReviewedByUser = users.FirstOrDefault(x => x.Id == approvalEntity.ReviewedByUserId)?.ToModel()
        };
    }

    protected override Task<List<Approval>> MapPageAsync(List<ApprovalEntity> items, ListApprovalRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<ApprovalEntity> OrderByExpression()
    {
        return new OrderByExpression<ApprovalEntity, DateTimeOffset>(x => x.CreatedAt);
    }

    protected override IQueryable<ApprovalEntity> FilterByPagedRequest(ListApprovalRequest listApprovalRequest)
    {
        var queryable = WithUsers()
            .Where(x => x.OrganizationId == listApprovalRequest.OrganizationId);

        if (listApprovalRequest.Status.HasValue)
        {
            queryable = queryable.Where(x => x.StatusId == (int)listApprovalRequest.Status.Value);
        }

        if (listApprovalRequest.Subject.HasValue)
        {
            queryable = queryable.Where(x => x.SubjectId == (int)listApprovalRequest.Subject.Value);
        }

        if (listApprovalRequest.RequestedByUserId.HasValue)
        {
            queryable = queryable.Where(x => x.RequestedByUserId == listApprovalRequest.RequestedByUserId.Value);
        }

        return queryable;
    }

    protected override IQueryable<ApprovalEntity> FilterByDateRange(IQueryable<ApprovalEntity> queryable, DatedRequest datedRequest)
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

    private IQueryable<ApprovalEntity> WithUsers()
    {
        return context.Approvals
            .Include(x => x.RequestedByUser)
            .Include(x => x.ReviewedByUser);
    }
}
