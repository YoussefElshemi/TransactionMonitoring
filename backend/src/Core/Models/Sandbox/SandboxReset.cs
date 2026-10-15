using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Sandbox;

public record SandboxReset
{
    public required OrganizationId OrganizationId { get; init; }
    public required Mode Mode { get; init; }
    public required List<SandboxResetCount> Counts { get; init; }
    public required Count TotalDeleted { get; init; }
    public required ResetAt ResetAt { get; init; }
}
