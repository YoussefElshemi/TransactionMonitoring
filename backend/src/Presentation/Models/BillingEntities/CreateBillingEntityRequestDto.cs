using Core.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.BillingEntities;

public record CreateBillingEntityRequestDto
{
    [FromBody]
    public required CreateBillingEntityRequestBodyDto Body { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid? CreatedByUserId { get; set; }
}

public record CreateBillingEntityRequestBodyDto
{
    public required string LegalName { get; init; }

    public required string Address { get; init; }

    public required string CountryCode { get; init; }

    public string? TaxIdentifier { get; init; }

    public required string InvoiceNumberPrefix { get; init; }
    public required string CreditNoteNumberPrefix { get; init; }

    public required Currency Currency { get; init; }
}
