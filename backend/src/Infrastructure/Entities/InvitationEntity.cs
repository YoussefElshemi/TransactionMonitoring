using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;
using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record InvitationEntity : BaseEntity, IAuditable
{
    public required Guid InvitationId { get; init; }
    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required string Email { get; init; }

    public required string NormalizedEmail { get; init; }

    public required Guid RoleId { get; init; }
    public ApplicationRoleEntity Role { get; init; } = null!;
    public required Guid? SubOrganizationId { get; init; }
    public SubOrganizationEntity? SubOrganization { get; init; }
    public required Guid InvitedByUserId { get; init; }
    public ApplicationUserEntity InvitedByUser { get; init; } = null!;
    public required string TokenHash { get; set; }
    public required DateTimeOffset ExpiresAt { get; set; }
    public required DateTimeOffset? AcceptedAt { get; set; }
    public required Guid? AcceptedByUserId { get; set; }
    public ApplicationUserEntity? AcceptedByUser { get; init; }
    public required DateTimeOffset? RevokedAt { get; set; }
    public required Guid? RevokedByUserId { get; set; }
    public ApplicationUserEntity? RevokedByUser { get; init; }

    [NotMapped]
    public Guid AuditEntityId => InvitationId;

    [NotMapped]
    public Guid? AuditOrganizationId => OrganizationId;

    [NotMapped]
    public AuditableEntityType AuditEntityType => AuditableEntityType.Invitation;

    [NotMapped]
    public IReadOnlyCollection<AuditedProperty> AuditedProperties => AuditedPropertyRegistry.Invitation;
}
