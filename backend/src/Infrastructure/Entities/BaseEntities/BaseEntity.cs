namespace Infrastructure.Entities.BaseEntities;

public record BaseEntity
{
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; set; }

    public uint RowVersion { get; init; }
}