using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;
using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record NotificationSubscriptionEntity : DeletableEntity, IAuditable, IModeScoped
{
    public required int ModeId { get; init; }

    public required Guid NotificationSubscriptionId { get; init; }
    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required int ChannelId { get; init; }
    public NotificationChannelEntity Channel { get; init; } = null!;
    public required string Destination { get; set; }
    public required int? MinimumSeverityId { get; set; }
    public RuleSeverityEntity? MinimumSeverity { get; init; }
    public required bool NotifyOnBreach { get; set; }
    public required bool IsEnabled { get; set; }
    public required string? Locale { get; set; }

    [NotMapped]
    public Guid AuditEntityId => NotificationSubscriptionId;

    [NotMapped]
    public Guid? AuditOrganizationId => OrganizationId;

    [NotMapped]
    public AuditableEntityType AuditEntityType => AuditableEntityType.NotificationSubscription;

    [NotMapped]
    public IReadOnlyCollection<AuditedProperty> AuditedProperties => AuditedPropertyRegistry.NotificationSubscription;
}
