using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record SimulationEntity : BaseEntity, IModeScoped
{
    public required int ModeId { get; init; }

    public required Guid SimulationId { get; init; }
    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required int ScenarioId { get; init; }
    public SimulationScenarioEntity ScenarioEntity { get; init; } = null!;
    public required int StatusId { get; set; }
    public SimulationStatusEntity StatusEntity { get; init; } = null!;
    public required int TotalPayments { get; init; }
    public required int ProcessedCount { get; set; }

    public required int Seed { get; init; }
    public required Guid CreatedByUserId { get; init; }
    public ApplicationUserEntity CreatedByUser { get; init; } = null!;
    public required DateTimeOffset? CompletedAt { get; set; }
    public required string? FailureReason { get; set; }
}
