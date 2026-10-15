using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record CaseDispositionEntity : BaseEntity
{
    public required int CaseDispositionId { get; init; }
    public required string Name { get; init; }
    public ICollection<CaseEntity> Cases { get; init; } = [];
    public ICollection<CaseReasonCodeEntity> CaseReasonCodes { get; init; } = [];
}
