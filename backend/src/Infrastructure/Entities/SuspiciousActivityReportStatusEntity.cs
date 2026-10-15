using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record SuspiciousActivityReportStatusEntity : BaseEntity
{
    public required int SuspiciousActivityReportStatusId { get; init; }
    public required string Name { get; init; }
    public ICollection<SuspiciousActivityReportEntity> SuspiciousActivityReports { get; init; } = [];
}
