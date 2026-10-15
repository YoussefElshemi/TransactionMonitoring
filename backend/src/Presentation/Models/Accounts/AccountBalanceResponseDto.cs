namespace Presentation.Models.Accounts;

public record AccountBalanceResponseDto
{
    public required Guid OrganizationId { get; init; }
    public required string Currency { get; init; }
    public required decimal Amount { get; init; }

    public required int EntryCount { get; init; }
}
