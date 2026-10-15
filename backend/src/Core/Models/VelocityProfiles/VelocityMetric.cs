using Core.ValueObjects;

namespace Core.Models.VelocityProfiles;

public record VelocityMetric
{
    public required VelocityMetricName Name { get; init; }
    public required VelocityMetricPath Path { get; init; }
}
