namespace Infrastructure.Entities.BaseEntities;

public record ProcessableEntity : DeletableEntity
{
    public required bool IsProcessed { get; set; }
    public required DateTimeOffset? ProcessedAt { get; set; }
}