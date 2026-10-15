using Microsoft.AspNetCore.Mvc;

namespace Presentation.Models.Common;

public record DatedRequestDto
{
    [FromQuery]
    public DateOnly FromDate { get; init; } = DateOnly.MinValue;

    [FromQuery]
    public DateOnly ToDate { get; init; } = DateOnly.MaxValue;
}