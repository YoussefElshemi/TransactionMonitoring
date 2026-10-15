using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.ServiceCredits;

public record IssueServiceCreditRequestDto
{
    [FromRoute]
    public required Guid OrganizationId { get; init; }

    [FromBody]
    public required IssueServiceCreditRequestBodyDto Body { get; init; }
}

public record IssueServiceCreditRequestBodyDto
{
    public required int Month { get; init; }
    public required int Year { get; init; }
}
