using Core.Models.Users;
using Presentation.Models.Users;

namespace Presentation.Mappers.Users;

public static class UserDtoMapper
{
    public static UserResponseDto ToDto(this ApplicationUser user)
    {
        return new UserResponseDto
        {
            UserId = user.UserId,
            Roles = user.Roles.Select(x => x.ToString()).ToArray(),
            SubOrganizationId = user.SubOrganizationId.HasValue ? (Guid)user.SubOrganizationId.Value : null,
            IsEnabled = user.IsEnabled,
            OrganizationId = user.OrganizationId,
            Email = user.Email,
            UserName = user.UserName,
            IsEmailConfirmed = user.EmailConfirmed,
            Locale = user.Locale?.ToString(),
            TimeZone = user.TimeZone?.ToString(),
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}
