using Core.Models.Users;
using Presentation.Mappers.Organizations;
using Presentation.Mappers.Users;
using Presentation.Models.Users;

namespace Presentation.Factories;

public class UserResponseFactory
{
    public UserResourceDto Create(ApplicationUser user, EnrichmentContext context)
    {
        return new UserResourceDto
        {
            User = user.ToDto(),
            Organization = context.CanDisclose(user.OrganizationId) ? user.Organization.ToDto() : null
        };
    }

    public UserResourceDto[] Create(IReadOnlyCollection<ApplicationUser> users, EnrichmentContext context)
    {
        return users.Select(x => Create(x, context)).ToArray();
    }
}
