using Infrastructure.Entities.BaseEntities;

namespace Infrastructure.Entities;

public record ApiClientScopeEntity : BaseEntity
{
    public required Guid ApiClientId { get; init; }
    public ApiClientEntity ApiClient { get; init; } = null!;
    public required int ApiScopeId { get; init; }
    public ApiScopeEntity ApiScope { get; init; } = null!;
}
