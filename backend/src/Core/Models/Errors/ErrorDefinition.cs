using Core.ValueObjects;

namespace Core.Models.Errors;

public record ErrorDefinition
{
    public required ErrorCode Code { get; init; }
    public required StatusCode Status { get; init; }
    public required ErrorDescription Description { get; init; }
}
