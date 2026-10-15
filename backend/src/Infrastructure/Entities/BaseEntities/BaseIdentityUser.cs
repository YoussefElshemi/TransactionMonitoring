using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Entities.BaseEntities;

public class BaseIdentityUser : IdentityUser<Guid>
{
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }

    public uint RowVersion { get; init; }
}