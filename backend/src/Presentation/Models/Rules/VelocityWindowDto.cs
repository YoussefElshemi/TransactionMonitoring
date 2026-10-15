namespace Presentation.Models.Rules;

public record VelocityWindowDto
{
    public required string Kind { get; init; }

    public required string Unit { get; init; }

    public int? Length { get; init; }

    public string? TimeZone { get; init; }
}
