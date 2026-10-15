using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;
using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record IncidentEntity : DeletableEntity, IAuditable
{
    public required Guid IncidentId { get; init; }
    public required string IncidentNumber { get; init; }
    public required string Title { get; set; }
    public required string Summary { get; set; }
    public required int StatusId { get; set; }
    public IncidentStatusEntity Status { get; init; } = null!;
    public required int ImpactId { get; set; }
    public IncidentImpactEntity Impact { get; init; } = null!;
    public required DateTimeOffset StartedAt { get; set; }
    public required DateTimeOffset? ResolvedAt { get; set; }
    public required string? RootCause { get; set; }
    public required string? Remediation { get; set; }
    public required DateTimeOffset? ReviewPublishedAt { get; set; }
    public required Guid? CreatedByUserId { get; init; }
    public ApplicationUserEntity? CreatedByUser { get; init; }
    public ICollection<IncidentComponentEntity> Components { get; init; } = [];
    public ICollection<IncidentUpdateEntity> Updates { get; init; } = [];

    [NotMapped]
    public Guid AuditEntityId => IncidentId;

    [NotMapped]
    public Guid? AuditOrganizationId => null;

    [NotMapped]
    public AuditableEntityType AuditEntityType => AuditableEntityType.Incident;

    [NotMapped]
    public IReadOnlyCollection<AuditedProperty> AuditedProperties => AuditedPropertyRegistry.Incident;
}
