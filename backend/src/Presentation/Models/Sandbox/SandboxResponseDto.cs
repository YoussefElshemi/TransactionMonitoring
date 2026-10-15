namespace Presentation.Models.Sandbox;

public record SandboxCreditResponseDto
{
    public required Guid OrganizationId { get; init; }
    public required int Allowance { get; init; }
    public required int Used { get; init; }
    public required int Remaining { get; init; }
    public required DateTimeOffset ResetsAt { get; init; }

    public required string Unit { get; init; }
}

public record SandboxResetCountDto
{
    public required string Resource { get; init; }
    public required int Count { get; init; }
}

public record SandboxResetResponseDto
{
    public required Guid OrganizationId { get; init; }
    public required string Mode { get; init; }
    public required SandboxResetCountDto[] Counts { get; init; }
    public required int TotalDeleted { get; init; }
    public required DateTimeOffset ResetAt { get; init; }
}
