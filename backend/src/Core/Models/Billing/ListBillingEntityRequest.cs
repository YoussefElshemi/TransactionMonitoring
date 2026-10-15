using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.Billing;

public record ListBillingEntityRequest : PagedRequest
{
    public required IncludeDisabled IncludeDisabled { get; init; }
}
