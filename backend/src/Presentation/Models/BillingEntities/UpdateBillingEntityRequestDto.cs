using Core.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.BillingEntities;

public record UpdateBillingEntityRequestDto
{
    [FromRoute]
    public required Guid BillingEntityId { get; init; }

    [FromBody]
    public required UpdateBillingEntityRequestBodyDto Body { get; init; }
}

public record UpdateBillingEntityRequestBodyDto
{
    public string? LegalName { get; init; }
    public string? Address { get; init; }
    public string? CountryCode { get; init; }
    public string? TaxIdentifier { get; init; }
    public Currency? Currency { get; init; }
    public bool? IsEnabled { get; init; }
}
