using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.Alerts;

// Separate type: two validators over one DTO type fight over IValidator<T>.
public record ListCaseAlertRequestDto : ListAlertRequestDto
{
    [FromRoute]
    public required Guid RouteCaseId { get; init; }
}
