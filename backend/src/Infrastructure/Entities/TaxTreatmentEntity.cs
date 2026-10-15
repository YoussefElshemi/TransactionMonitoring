using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record TaxTreatmentEntity : BaseEntity
{
    public required int TaxTreatmentId { get; init; }
    public required string Name { get; init; }
}
