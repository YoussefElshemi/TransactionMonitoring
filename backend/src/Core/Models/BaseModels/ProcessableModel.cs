using Core.ValueObjects;

namespace Core.Models.BaseModels;

public record ProcessableModel : DeletableModel
{
    public required IsProcessed IsProcessed { get; init; }
    public required ProcessedAt? ProcessedAt { get; init; }
}