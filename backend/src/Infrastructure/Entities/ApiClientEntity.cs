using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;
using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record ApiClientEntity : DeletableEntity, IAuditable, IModeScoped
{
    public required int ModeId { get; init; }

    public required Guid ApiClientId { get; init; }
    public required string ClientId { get; init; }
    public required string FriendlyName { get; set; }
    public required bool IsEnabled { get; set; }
    public required string ApiVersion { get; set; }
    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required Guid CreatedByUserId { get; init; }
    public ApplicationUserEntity CreatedByUser { get; init; } = null!;
    public ICollection<ApiClientScopeEntity> Scopes { get; init; } = [];
    public ICollection<ApiClientSecretEntity> Secrets { get; init; } = [];

    [NotMapped]
    public Guid AuditEntityId => ApiClientId;

    [NotMapped]
    public Guid? AuditOrganizationId => OrganizationId;

    [NotMapped]
    public AuditableEntityType AuditEntityType => AuditableEntityType.ApiClient;

    [NotMapped]
    public IReadOnlyCollection<AuditedProperty> AuditedProperties => AuditedPropertyRegistry.ApiClient;
}
