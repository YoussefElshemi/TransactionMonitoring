using Core.ValueObjects;

namespace Core.Models.Users;

public record OffboardUserRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required ApplicationUserId UserId { get; init; }

    public required ApplicationUserId? SuccessorUserId { get; init; }
    public required ApplicationUserId OffboardedByUserId { get; init; }
}
