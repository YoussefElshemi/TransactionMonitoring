using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;
using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record ApiClientSecretEntity : DeletableEntity, IAuditable, IModeScoped
{
    public required int ModeId { get; init; }

    public required Guid ApiClientSecretId { get; init; }
    public required Guid ApiClientId { get; init; }
    public ApiClientEntity ApiClient { get; init; } = null!;
    public required Guid OrganizationId { get; init; }
    public required string SecretHash { get; init; }
    public required string SecretPrefix { get; init; }
    public required DateTimeOffset? ExpiresAt { get; init; }
    public required DateTimeOffset? RevokedAt { get; set; }
    public required DateTimeOffset? LastUsedAt { get; init; }
    public required Guid CreatedByUserId { get; init; }
    public ApplicationUserEntity CreatedByUser { get; init; } = null!;

    [NotMapped]
    public Guid AuditEntityId => ApiClientSecretId;

    [NotMapped]
    public Guid? AuditOrganizationId => OrganizationId;

    [NotMapped]
    public AuditableEntityType AuditEntityType => AuditableEntityType.ApiClientSecret;

    [NotMapped]
    public IReadOnlyCollection<AuditedProperty> AuditedProperties => AuditedPropertyRegistry.ApiClientSecret;
}
