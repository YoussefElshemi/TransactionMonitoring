using Core.ValueObjects;

namespace Core.Models.Sandbox;

public record SandboxResetCount
{
    public required Resource Resource { get; init; }
    public required Count Count { get; init; }
}
