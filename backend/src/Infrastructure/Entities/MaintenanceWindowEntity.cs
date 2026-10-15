using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;
using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record MaintenanceWindowEntity : DeletableEntity, IAuditable
{
    public required Guid MaintenanceWindowId { get; init; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required int StatusId { get; set; }
    public MaintenanceStatusEntity Status { get; init; } = null!;
    public required DateTimeOffset ScheduledStart { get; set; }
    public required DateTimeOffset ScheduledEnd { get; set; }
    public required DateTimeOffset? AnnouncedAt { get; set; }
    public required Guid? CreatedByUserId { get; init; }
    public ApplicationUserEntity? CreatedByUser { get; init; }
    public ICollection<MaintenanceWindowComponentEntity> Components { get; init; } = [];

    [NotMapped]
    public Guid AuditEntityId => MaintenanceWindowId;

    [NotMapped]
    public Guid? AuditOrganizationId => null;

    [NotMapped]
    public AuditableEntityType AuditEntityType => AuditableEntityType.MaintenanceWindow;

    [NotMapped]
    public IReadOnlyCollection<AuditedProperty> AuditedProperties => AuditedPropertyRegistry.MaintenanceWindow;
}
