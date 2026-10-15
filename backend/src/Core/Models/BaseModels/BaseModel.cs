using Core.ValueObjects;

namespace Core.Models.BaseModels;

public record BaseModel
{
    public required CreatedAt CreatedAt { get; init; }
    public required UpdatedAt UpdatedAt { get; init; }
}