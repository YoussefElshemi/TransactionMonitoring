using Core.Models.BaseModels;
using Core.ValueObjects;

namespace Core.Models.OrganizationDataKeys;

public record OrganizationDataKey : BaseModel
{
    public required OrganizationId OrganizationId { get; init; }
    public required WrappedDataKey WrappedDataKey { get; init; }
}
