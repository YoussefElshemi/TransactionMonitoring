using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.OrganizationInvoices;

public record GenerateOrganizationInvoiceRequestDto
{
    public required Guid OrganizationId { get; init; }

    [FromBody]
    public required GenerateOrganizationInvoiceRequestBodyDto Body { get; init; }
}

public record GenerateOrganizationInvoiceRequestBodyDto
{
    public required int Month { get; init; }
    public required int Year { get; init; }
}