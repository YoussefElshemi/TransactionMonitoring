using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Accounts;

public record AccountBalance
{
    public required OrganizationId OrganizationId { get; init; }
    public required Currency Currency { get; init; }
    public required BalanceAmount Amount { get; init; }

    public required Count EntryCount { get; init; }
}
