using Core.Interfaces.Repositories;
using Core.Models.Attachments;
using Core.Models.Common;
using Core.ValueObjects;
using Infrastructure.Entities;
using Infrastructure.Expressions;
using Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class AttachmentRepository(
    TransactionMonitoringDbContext context,
    TimeProvider timeProvider) : PagedRepository<ListAttachmentRequest, AttachmentEntity, Attachment>, IAttachmentRepository
{
    public async Task AddAsync(Attachment attachment, CancellationToken stoppingToken)
    {
        var attachmentEntity = attachment.ToEntity();
        await context.Attachments.AddAsync(attachmentEntity, stoppingToken);
    }

    // The only mutation following an insert in one unit of work: the added entity is still tracked, so update it rather than load a second instance.
    public async Task RecordScanResultAsync(AttachmentId attachmentId, ScanResult scanResult, CancellationToken stoppingToken)
    {
        var attachmentEntity =
            context.Attachments.Local.FirstOrDefault(x => x.AttachmentId == attachmentId) ??
            await context.Attachments.FirstAsync(x => x.AttachmentId == attachmentId, stoppingToken);

        attachmentEntity.ScanStatusId = (int)scanResult.ScanStatus;
        attachmentEntity.ThreatName = scanResult.ThreatName?.ToString();
        attachmentEntity.ScannedAt = timeProvider.GetUtcNow();
        attachmentEntity.UpdatedAt = timeProvider.GetUtcNow();

        context.Attachments.Update(attachmentEntity);
    }

    public async Task<Exists> ExistsAsync(OrganizationId organizationId, CaseId caseId, AttachmentId attachmentId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        var exists = await context.Attachments
            .AnyAsync(x => x.AttachmentId == attachmentId &&
                           x.CaseId == caseId &&
                           x.OrganizationId == organizationId &&
                           (includeDeleted || x.IsDeleted == false), stoppingToken);

        return new Exists(exists);
    }

    public async Task<Attachment> GetAsync(AttachmentId attachmentId, CancellationToken stoppingToken)
    {
        var attachmentEntity = await context.Attachments
            .FirstAsync(x => x.AttachmentId == attachmentId && !x.IsDeleted, stoppingToken);

        return attachmentEntity.ToModel();
    }

    public async Task DeleteAsync(AttachmentId attachmentId, CancellationToken stoppingToken)
    {
        var attachmentEntity = await context.Attachments
            .FirstAsync(x => x.AttachmentId == attachmentId, stoppingToken);

        attachmentEntity.IsDeleted = true;
        attachmentEntity.DeletedAt = timeProvider.GetUtcNow();

        context.Attachments.Update(attachmentEntity);
    }

    public async Task RecoverAsync(AttachmentId attachmentId, CancellationToken stoppingToken)
    {
        var attachmentEntity = await context.Attachments
            .FirstAsync(x => x.AttachmentId == attachmentId, stoppingToken);

        attachmentEntity.IsDeleted = false;
        attachmentEntity.DeletedAt = null;

        context.Attachments.Update(attachmentEntity);
    }

    protected override Task<List<Attachment>> MapPageAsync(List<AttachmentEntity> items, ListAttachmentRequest request, CancellationToken stoppingToken)
    {
        return Task.FromResult(items.ConvertAll(x => x.ToModel()));
    }

    protected override IOrderByExpression<AttachmentEntity> OrderByExpression()
    {
        return new OrderByExpression<AttachmentEntity, DateTimeOffset>(x => x.UpdatedAt);
    }

    protected override IQueryable<AttachmentEntity> FilterByPagedRequest(ListAttachmentRequest listAttachmentRequest)
    {
        var queryable = context.Attachments
            .Where(x => x.CaseId == listAttachmentRequest.CaseId &&
                        x.OrganizationId == listAttachmentRequest.OrganizationId &&
                        !x.IsDeleted);

        if (listAttachmentRequest.FileName.HasValue)
        {
            queryable = queryable
                .Where(x => EF.Functions.ILike(x.FileName, $"%{listAttachmentRequest.FileName}%"));
        }

        if (listAttachmentRequest.ScanStatus.HasValue)
        {
            queryable = queryable
                .Where(x => x.ScanStatusId == (int)listAttachmentRequest.ScanStatus.Value);
        }

        return queryable;
    }

    protected override IQueryable<AttachmentEntity> FilterByDateRange(IQueryable<AttachmentEntity> queryable, DatedRequest datedRequest)
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
