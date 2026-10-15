namespace Presentation.Models.ApiClients;

public record ApiClientResponseDto
{
    public required Guid ApiClientId { get; init; }
    public required string ClientId { get; init; }
    public required string FriendlyName { get; init; }
    public required bool IsEnabled { get; init; }
    public required string[] Scopes { get; init; }
    public required Guid OrganizationId { get; init; }
    public required Guid CreatedByUserId { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }

    public required string Mode { get; init; }
    public required string ApiVersion { get; init; }
}

public record CreatedApiClientResponseDto : ApiClientResponseDto
{
    public required string ClientSecret { get; init; }
}
