using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record IncidentUpdateEntity : BaseEntity
{
    public required Guid IncidentUpdateId { get; init; }
    public required Guid IncidentId { get; init; }
    public IncidentEntity Incident { get; init; } = null!;
    public required int StatusId { get; init; }
    public IncidentStatusEntity Status { get; init; } = null!;
    public required string Body { get; init; }
    public required DateTimeOffset PostedAt { get; init; }
    public required Guid? PostedByUserId { get; init; }
    public ApplicationUserEntity? PostedByUser { get; init; }
}
