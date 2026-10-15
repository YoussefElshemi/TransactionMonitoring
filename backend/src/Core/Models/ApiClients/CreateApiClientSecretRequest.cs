using Core.ValueObjects;

namespace Core.Models.ApiClients;

public record CreateApiClientSecretRequest
{
    public required ApiClientId ApiClientId { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required ApplicationUserId UserId { get; init; }
    public required ExpiresAt? ExpiresAt { get; init; }
}
