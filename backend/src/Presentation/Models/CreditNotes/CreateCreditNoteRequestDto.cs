using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

namespace Presentation.Models.CreditNotes;

public record CreateCreditNoteRequestDto
{
    [FromRoute]
    public required Guid OrganizationId { get; init; }

    [FromRoute]
    public required Guid InvoiceId { get; init; }

    [FromBody]
    public required CreateCreditNoteRequestBodyDto Body { get; init; }

    [BindNever]
    [SwaggerSchema(ReadOnly = true)]
    public Guid? CreatedByUserId { get; set; }
}

public record CreateCreditNoteRequestBodyDto
{
    public required string Reason { get; init; }

    public decimal? NetAmount { get; init; }
}
