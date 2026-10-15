using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Rules;

public record VelocityWindow
{
    public required VelocityWindowKind Kind { get; init; }
    public required VelocityWindowUnit Unit { get; init; }

    public required WindowLength? Length { get; init; }

    public required WindowTimeZone? TimeZone { get; init; }
}
