using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record ApiScopeEntity : BaseEntity
{
    public required int ApiScopeId { get; init; }
    public required string Name { get; init; }
    public ICollection<ApiClientScopeEntity> ApiClientScopes { get; init; } = [];
}
