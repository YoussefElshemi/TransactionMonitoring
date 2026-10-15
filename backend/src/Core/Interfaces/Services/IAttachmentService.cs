using Core.Models.Attachments;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IAttachmentService : IPagedService<ListAttachmentRequest, Attachment>
{
    Task<Attachment> CreateAsync(CreateAttachmentRequest createAttachmentRequest, CancellationToken stoppingToken);
    Task<AttachmentContent> DownloadAsync(AttachmentId attachmentId, CancellationToken stoppingToken);
    Task<Attachment> RescanAsync(AttachmentId attachmentId, CancellationToken stoppingToken);
    Task<Exists> ExistsAsync(OrganizationId organizationId, CaseId caseId, AttachmentId attachmentId, IncludeDeleted includeDeleted, CancellationToken stoppingToken);
    Task<Attachment> GetAsync(AttachmentId attachmentId, CancellationToken stoppingToken);
    Task DeleteAsync(AttachmentId attachmentId, CancellationToken stoppingToken);
    Task RecoverAsync(AttachmentId attachmentId, CancellationToken stoppingToken);
}
