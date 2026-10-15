using Core.ValueObjects;

namespace Core.Models.Users;

public record CreateApplicationUserRequest
{
    public required ApplicationUserId UserId { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required SubOrganizationId? SubOrganizationId { get; init; }
    public required Email Email { get; init; }
    public required UserName UserName { get; init; }
    public required Password Password { get; init; }

    public required EmailConfirmed EmailConfirmed { get; init; }
}
