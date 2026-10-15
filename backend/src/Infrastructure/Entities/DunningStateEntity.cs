using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record DunningStateEntity : BaseEntity
{
    public required Guid DunningStateId { get; init; }
    public required Guid InvoiceId { get; init; }
    public InvoiceEntity Invoice { get; init; } = null!;
    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;

    public required int DunningStageId { get; set; }
    public DunningStageEntity DunningStage { get; init; } = null!;

    public required DateTimeOffset NextAttemptAt { get; set; }
    public required DateTimeOffset? LastNoticedAt { get; set; }
    public required int CollectionAttempts { get; set; }
    public required DateTimeOffset? LastAttemptedAt { get; set; }
    public required DateTimeOffset? RestrictionProposedAt { get; set; }

    public required DateTimeOffset? ResolvedAt { get; set; }
}
