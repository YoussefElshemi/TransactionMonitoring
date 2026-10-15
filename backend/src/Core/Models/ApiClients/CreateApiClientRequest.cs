using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.ApiClients;

public record CreateApiClientRequest
{
    public required FriendlyName FriendlyName { get; init; }
    public required ApplicationUserId UserId { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required List<ApiScope> Scopes { get; init; }
}
