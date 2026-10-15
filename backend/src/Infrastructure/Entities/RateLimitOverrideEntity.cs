using System.ComponentModel.DataAnnotations.Schema;
using Core.Enums;
using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record RateLimitOverrideEntity : DeletableEntity, IAuditable
{
    public required Guid RateLimitOverrideId { get; init; }
    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required int? PoolId { get; init; }
    public RateLimitPoolEntity? PoolEntity { get; init; }
    public required int PermitLimit { get; init; }
    public required int TokensPerPeriod { get; init; }
    public required int ReplenishmentPeriodSeconds { get; init; }
    public required string Reason { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
    public required Guid CreatedByUserId { get; init; }

    [NotMapped]
    public Guid AuditEntityId => RateLimitOverrideId;

    [NotMapped]
    public Guid? AuditOrganizationId => OrganizationId;

    [NotMapped]
    public AuditableEntityType AuditEntityType => AuditableEntityType.RateLimitOverride;

    [NotMapped]
    public IReadOnlyCollection<AuditedProperty> AuditedProperties => AuditedPropertyRegistry.RateLimitOverride;
}
