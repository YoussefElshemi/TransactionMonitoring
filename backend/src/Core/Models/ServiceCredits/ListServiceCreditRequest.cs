using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.ServiceCredits;

public record ListServiceCreditRequest : PagedRequest
{
    public required OrganizationId OrganizationId { get; init; }

    public required IncludeMet IncludeMet { get; init; }
}
