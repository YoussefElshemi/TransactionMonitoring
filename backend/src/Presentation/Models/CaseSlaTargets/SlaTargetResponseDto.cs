namespace Presentation.Models.CaseSlaTargets;

public record SlaTargetResponseDto
{
    public required string Severity { get; init; }
    public required int TargetMinutes { get; init; }
}
