using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record VelocityBackfillEntity : BaseEntity, IModeScoped
{
    public required int ModeId { get; init; }

    public required Guid VelocityBackfillId { get; init; }
    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required string Metrics { get; init; }
    public required int StatusId { get; set; }
    public VelocityBackfillStatusEntity StatusEntity { get; init; } = null!;
    public required int TotalTransactions { get; set; }
    public required int ProcessedCount { get; set; }
    public required DateTimeOffset? CompletedAt { get; set; }
}
