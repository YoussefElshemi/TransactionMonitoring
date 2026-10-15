using Core.ValueObjects;

namespace Core.Models.VelocityProfiles;

public record VelocityProfileUsage
{
    public required RuleId RuleId { get; init; }
    public required FriendlyName FriendlyName { get; init; }
    public required IsEnabled IsEnabled { get; init; }
    public required List<VelocityMetricName> Metrics { get; init; }
    public required List<VelocityThresholdName> Thresholds { get; init; }
}
