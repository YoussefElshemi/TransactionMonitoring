using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;
using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record WebhookConfigurationEntity : DeletableEntity, IAuditable, IModeScoped
{
    public required int ModeId { get; init; }

    public required Guid WebhookConfigurationId { get; init; }
    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required string EndpointUrl { get; set; }
    public required string SecretKey { get; set; }
    public DateTimeOffset? SecretKeyRotatedAt { get; set; }
    public required bool IsEnabled { get; set; }
    public required int PayloadVersion { get; set; }
    public required int ConsecutiveFailures { get; set; }
    public DateTimeOffset? LastSucceededAt { get; set; }
    public DateTimeOffset? LastFailedAt { get; set; }
    public DateTimeOffset? DisabledAt { get; set; }
    public string? DisabledReason { get; set; }

    public ICollection<WebhookConfigurationEventEntity> SubscribedEvents { get; init; } = [];

    public ICollection<OutboxMessageEntity> OutboxMessages { get; init; } = [];

    [NotMapped]
    public Guid AuditEntityId => WebhookConfigurationId;

    [NotMapped]
    public Guid? AuditOrganizationId => OrganizationId;

    [NotMapped]
    public AuditableEntityType AuditEntityType => AuditableEntityType.WebhookConfiguration;

    [NotMapped]
    public IReadOnlyCollection<AuditedProperty> AuditedProperties => AuditedPropertyRegistry.WebhookConfiguration;
}
