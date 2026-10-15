using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record CaseSlaTargetEntity : BaseEntity, IModeScoped
{
    public required int ModeId { get; init; }

    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required int SeverityId { get; init; }
    public RuleSeverityEntity SeverityEntity { get; init; } = null!;
    public required int TargetMinutes { get; init; }
}
