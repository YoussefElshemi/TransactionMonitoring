using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.Users;

public record UserRequestDto
{
    [FromRoute]
    public required Guid UserId { get; init; }
}