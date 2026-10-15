using Core.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.BillingProfiles;

public record SaveBillingProfileRequestDto
{
    [FromRoute]
    public required Guid OrganizationId { get; init; }

    [FromBody]
    public required SaveBillingProfileRequestBodyDto Body { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid? UpdatedByUserId { get; set; }
}

public record SaveBillingProfileRequestBodyDto
{
    public required Guid BillingEntityId { get; init; }

    public required string LegalName { get; init; }
    public required string ContactName { get; init; }
    public required string ContactEmail { get; init; }
    public required string Address { get; init; }

    public required string CountryCode { get; init; }

    public string? TaxIdentifier { get; init; }

    public required Currency Currency { get; init; }

    public required int PaymentTerms { get; init; }

    public string? Locale { get; init; }
}
