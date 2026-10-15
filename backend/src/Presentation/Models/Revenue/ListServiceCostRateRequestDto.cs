using Core.Enums;
using Presentation.Models.Common;

namespace Presentation.Models.Revenue;

public record ListServiceCostRateRequestDto : PagedRequestDto
{
    public BillableResource? BillableResource { get; init; }
}
