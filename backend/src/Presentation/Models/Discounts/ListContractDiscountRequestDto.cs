using Microsoft.AspNetCore.Mvc;
using Presentation.Models.Common;

namespace Presentation.Models.Discounts;

public record ListContractDiscountRequestDto : PagedRequestDto
{
    [FromRoute]
    public new required Guid OrganizationId
    {
        get => base.OrganizationId;
        init => base.OrganizationId = value;
    }

    public bool? IncludeExpired { get; init; }
}
