namespace Infrastructure.Entities.BaseEntities;

public class DeletableIdentityUser : BaseIdentityUser
{
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}