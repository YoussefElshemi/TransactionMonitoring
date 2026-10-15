using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record InvoiceStatusEntity : BaseEntity
{
    public required int InvoiceStatusId { get; init; }
    public required string Name { get; init; }
}
