using System.Text.Json.Serialization;

namespace Presentation.Models.Parties;

public record TransactionPartyResponseDto
{
    public required Guid TransactionId { get; init; }
    public required Guid PartyId { get; init; }
    public required string Role { get; init; }
    public required string? Identifier { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DisplayName { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}
