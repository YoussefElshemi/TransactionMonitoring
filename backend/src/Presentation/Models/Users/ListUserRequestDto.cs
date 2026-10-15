using Presentation.Models.Common;

namespace Presentation.Models.Users;

public record ListUserRequestDto : PagedRequestDto
{
    public string? UserName { get; init; }
}