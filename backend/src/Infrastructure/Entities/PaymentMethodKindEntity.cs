using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record PaymentMethodKindEntity : BaseEntity
{
    public required int PaymentMethodKindId { get; init; }
    public required string Name { get; init; }
}
