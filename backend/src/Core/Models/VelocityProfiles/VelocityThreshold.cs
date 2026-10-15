using Core.ValueObjects;

namespace Core.Models.VelocityProfiles;

public record VelocityThreshold
{
    public required VelocityThresholdName Name { get; init; }
    public required VelocityThresholdValue Value { get; init; }
}
