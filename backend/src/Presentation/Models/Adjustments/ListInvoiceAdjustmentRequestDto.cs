using Microsoft.AspNetCore.Mvc;
using Presentation.Models.Common;

namespace Presentation.Models.Adjustments;

public record ListInvoiceAdjustmentRequestDto : PagedRequestDto
{
    [FromRoute]
    public new required Guid OrganizationId
    {
        get => base.OrganizationId;
        init => base.OrganizationId = value;
    }

    public bool? IncludeApplied { get; init; }
}
