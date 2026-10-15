using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record InvoiceLineTypeEntity : BaseEntity
{
    public required int InvoiceLineTypeId { get; init; }
    public required string Name { get; init; }
}
