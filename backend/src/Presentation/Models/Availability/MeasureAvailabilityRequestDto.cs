using Microsoft.AspNetCore.Mvc;
using Presentation.Models.Common;

namespace Presentation.Models.Availability;

public record MeasureAvailabilityRequestDto : DatedRequestDto
{
    [FromRoute]
    public required Guid OrganizationId { get; init; }
}
