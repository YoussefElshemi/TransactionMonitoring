using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record PaymentStatusEntity : BaseEntity
{
    public required int PaymentStatusId { get; init; }
    public required string Name { get; init; }
}
