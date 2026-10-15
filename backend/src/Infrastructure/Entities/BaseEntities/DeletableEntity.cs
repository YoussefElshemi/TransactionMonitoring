namespace Infrastructure.Entities.BaseEntities;

public record DeletableEntity : BaseEntity
{
    public required bool IsDeleted { get; set; }
    public required DateTimeOffset? DeletedAt { get; set; }
}