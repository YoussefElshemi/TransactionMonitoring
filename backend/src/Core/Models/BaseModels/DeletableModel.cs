using Core.ValueObjects;

namespace Core.Models.BaseModels;

public record DeletableModel : BaseModel
{
    public required IsDeleted IsDeleted { get; init; }
    public required DeletedAt? DeletedAt { get; init; }
}