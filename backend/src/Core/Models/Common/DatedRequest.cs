using Core.ValueObjects;

namespace Core.Models.Common;

public record DatedRequest
{
    public required DateRange FromDate { get; init; }
    public required DateRange ToDate { get; init; }
}