namespace Presentation.Models.ApiClients;

public record ApiClientSecretResponseDto
{
    public required Guid ApiClientSecretId { get; init; }
    public required Guid ApiClientId { get; init; }

    public required string SecretPrefix { get; init; }

    public required DateTimeOffset? ExpiresAt { get; init; }
    public required DateTimeOffset? RevokedAt { get; init; }
    public required DateTimeOffset? LastUsedAt { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }

    public required string Mode { get; init; }
}

public record CreatedApiClientSecretResponseDto : ApiClientSecretResponseDto
{
    public required string ClientSecret { get; init; }
}
