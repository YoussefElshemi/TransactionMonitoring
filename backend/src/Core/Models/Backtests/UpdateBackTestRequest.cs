using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.BackTests;

public record UpdateBackTestRequest
{
    public required BackTestId BackTestId { get; init; }
    public required BackTestStatus Status { get; init; }
    public required TotalCount TotalEvaluations { get; init; }
    public required ProcessedCount ProcessedCount { get; init; }
    public required CompletedAt? CompletedAt { get; init; }
}
