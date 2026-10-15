using Core.ValueObjects;

namespace Core.Models.Sandbox;

public record SandboxCredit
{
    public required OrganizationId OrganizationId { get; init; }
    public required SandboxCredits Allowance { get; init; }
    public required Count Used { get; init; }
    public required Count Remaining { get; init; }

    public required ResetAt ResetsAt { get; init; }
}
