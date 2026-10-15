using Microsoft.AspNetCore.Mvc;
using Presentation.Models.Invoices;

namespace Presentation.Models.OrganizationInvoices;

public record ListOrganizationInvoiceRequestDto : ListInvoiceRequestDto
{
    [FromRoute]
    public new required Guid OrganizationId
    {
        get => base.OrganizationId;
        init => base.OrganizationId = value;
    }
}