using Presentation.Models.Common;

namespace Presentation.Models.Usages;

public record ListUsageRequestDto : PagedRequestDto
{
    public string? Resource { get; init; }
}