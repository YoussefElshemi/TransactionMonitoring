using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record SuspiciousActivityReportPolicyEntity : BaseEntity, IModeScoped
{
    public required int ModeId { get; init; }

    public required Guid OrganizationId { get; init; }
    public OrganizationEntity Organization { get; init; } = null!;
    public required string? ReportingEntityId { get; set; }
    public required int? FilingDeadlineDays { get; set; }
    public required bool RequireFilingApproval { get; set; }
    public required string? AmountPath { get; set; }
    public required string? CurrencyPath { get; set; }
}
