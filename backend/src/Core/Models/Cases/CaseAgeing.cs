using Core.Enums;
using Core.ValueObjects;

namespace Core.Models.Cases;

public record CaseAgeing
{
    public required TimeSpan Age { get; init; }

    public required TimeSpan TimeToDue { get; init; }
    public required CaseAgeBucket Bucket { get; init; }
    public required IsBreached IsBreached { get; init; }
}
