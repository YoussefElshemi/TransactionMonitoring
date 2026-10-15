using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.Accounts;

public record ListAccountEntryRequest : PagedRequest
{
    public required OrganizationId OrganizationId { get; init; }
}
