using Core.Enums;
using Core.Models.BaseModels;
using Core.ValueObjects;

namespace Core.Models.ApiClients;

public record ApiClientSecret : DeletableModel
{
    public required ApiClientSecretId ApiClientSecretId { get; init; }
    public required ApiClientId ApiClientId { get; init; }
    public required OrganizationId OrganizationId { get; init; }
    public required SecretHash SecretHash { get; init; }
    public required SecretPrefix SecretPrefix { get; init; }
    public required ExpiresAt? ExpiresAt { get; init; }
    public required RevokedAt? RevokedAt { get; init; }
    public required LastUsedAt? LastUsedAt { get; init; }
    public required ApplicationUserId CreatedByUserId { get; init; }

    public required Mode Mode { get; init; }
}
