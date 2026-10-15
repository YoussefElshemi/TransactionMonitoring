using Core.Models.Attachments;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IAttachmentStore
{
    Task<StoredAttachment> WriteAsync(OrganizationId organizationId, AttachmentId attachmentId, Stream content, CancellationToken stoppingToken);
    Task<Stream> ReadAsync(StorageKey storageKey, CancellationToken stoppingToken);
    Task DeleteAsync(StorageKey storageKey, CancellationToken stoppingToken);
}
