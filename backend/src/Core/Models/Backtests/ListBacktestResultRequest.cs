using Core.Models.Common;
using Core.ValueObjects;

namespace Core.Models.BackTests;

public record ListBackTestResultRequest : PagedRequest
{
    public required BackTestId BackTestId { get; init; }
    public required bool? ChangedOnly { get; init; }
}
