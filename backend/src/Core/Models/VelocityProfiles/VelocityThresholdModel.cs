namespace Core.Models.VelocityProfiles;

public record VelocityThresholdModel
{
    public string? Name { get; init; }

    public decimal Value { get; init; }
}
