using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record AttachmentScanStatusEntity : BaseEntity
{
    public required int AttachmentScanStatusId { get; init; }
    public required string Name { get; init; }
    public ICollection<AttachmentEntity> Attachments { get; init; } = [];
}
