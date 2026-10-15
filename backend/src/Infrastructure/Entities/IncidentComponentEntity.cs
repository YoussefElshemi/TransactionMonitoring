using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record IncidentComponentEntity : BaseEntity
{
    public required Guid IncidentId { get; init; }
    public IncidentEntity Incident { get; init; } = null!;
    public required int ComponentId { get; init; }
    public ServiceComponentEntity Component { get; init; } = null!;
}
