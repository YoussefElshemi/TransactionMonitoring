using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record ComponentUptimeDayEntity : BaseEntity
{
    public required Guid ComponentUptimeDayId { get; init; }
    public required int ComponentId { get; init; }
    public ServiceComponentEntity Component { get; init; } = null!;
    public required DateOnly Date { get; init; }
    public required int TotalProbes { get; set; }
    public required int HealthyProbes { get; set; }
    public required int DowntimeSeconds { get; set; }
    public required decimal UptimePercentage { get; set; }
}
