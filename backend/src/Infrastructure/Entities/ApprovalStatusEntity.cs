using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record ApprovalStatusEntity : BaseEntity
{
    public required int ApprovalStatusId { get; init; }
    public required string Name { get; init; }
    public ICollection<ApprovalEntity> Approvals { get; init; } = [];
}
