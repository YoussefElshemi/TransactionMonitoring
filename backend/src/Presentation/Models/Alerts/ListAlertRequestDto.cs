using Presentation.Models.Common;

namespace Presentation.Models.Alerts;

public record ListAlertRequestDto : PagedRequestDto
{
    public string? FriendlyName { get; init; }
    public string? Status { get; init; }
    public string? Severity { get; init; }
    public Guid? RuleId { get; init; }
    public Guid? PartyId { get; init; }
    public Guid? CaseId { get; set; }
}
