using Core.Models.Attachments;
using Core.ValueObjects;

namespace Core.Interfaces.Repositories;

public interface IAttachmentRepository : IPagedRepository<ListAttachmentRequest, Attachment>
{
    Task AddAsync(Attachment attachment, CancellationToken stoppingToken);
    Task RecordScanResultAsync(AttachmentId attachmentId, ScanResult scanResult, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, CaseId caseId, AttachmentId attachmentId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task<Attachment> GetAsync(AttachmentId attachmentId, CancellationToken stoppingToken);
    Task DeleteAsync(AttachmentId attachmentId, CancellationToken stoppingToken);
    Task RecoverAsync(AttachmentId attachmentId, CancellationToken stoppingToken);
}
