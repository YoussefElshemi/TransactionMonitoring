using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.Users;

public record ListUserRequest : PagedRequest
{
    public required UserName? UserName { get; init; }
}