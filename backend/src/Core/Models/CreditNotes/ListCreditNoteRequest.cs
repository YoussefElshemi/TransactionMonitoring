using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.CreditNotes;

public record ListCreditNoteRequest : PagedRequest
{
    public required OrganizationId OrganizationId { get; init; }

    public required InvoiceId? InvoiceId { get; init; }
}
