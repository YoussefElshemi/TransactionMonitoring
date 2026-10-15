using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record ApprovalSubjectEntity : BaseEntity
{
    public required int ApprovalSubjectId { get; init; }
    public required string Name { get; init; }
    public ICollection<ApprovalEntity> Approvals { get; init; } = [];
}
