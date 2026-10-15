using Core.ValueObjects;

namespace Core.Models.ServiceCredits;

public record IssueServiceCreditRequest
{
    public required OrganizationId OrganizationId { get; init; }
    public required Month Month { get; init; }
    public required Year Year { get; init; }
}
