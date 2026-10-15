using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record BackTestEntity : BaseEntity, IModeScoped
{
    public required int ModeId { get; init; }

    public required Guid BackTestId { get; init; }
    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required Guid RuleId { get; init; }
    public RuleEntity Rule { get; init; } = null!;
    public required Guid RuleVersionId { get; init; }
    public RuleVersionEntity RuleVersion { get; init; } = null!;
    public required int StatusId { get; set; }
    public BackTestStatusEntity StatusEntity { get; init; } = null!;
    public required int SampleSize { get; init; }
    public required int TotalEvaluations { get; set; }
    public required int ProcessedCount { get; set; }
    public required Guid CreatedByUserId { get; init; }
    public ApplicationUserEntity CreatedByUser { get; init; } = null!;
    public required DateTimeOffset? CompletedAt { get; set; }
    public ICollection<BackTestResultEntity> Results { get; init; } = [];
}
