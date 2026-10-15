using Core.Enums;

namespace Core.Models.Rules;

public record VelocityWindowModel
{
    public VelocityWindowKind Kind { get; init; }

    public VelocityWindowUnit Unit { get; init; }

    public int? Length { get; init; }

    public string? TimeZone { get; init; }
}
