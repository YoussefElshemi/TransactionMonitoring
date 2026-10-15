using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.Discounts;

public record ListContractDiscountRequest : PagedRequest
{
    public required OrganizationId OrganizationId { get; init; }

    public required IncludeExpired IncludeExpired { get; init; }
}
