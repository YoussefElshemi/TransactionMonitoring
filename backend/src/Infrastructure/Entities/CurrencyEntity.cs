using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record CurrencyEntity : BaseEntity
{
    public required int CurrencyId { get; init; }

    public required string Name { get; init; }
}
