namespace Presentation.Models.Cases;

public record CaseAgeingResponseDto
{
    public required double AgeInMilliseconds { get; init; }
    public required double TimeToDueInMilliseconds { get; init; }
    public required string Bucket { get; init; }
    public required bool IsBreached { get; init; }
}
