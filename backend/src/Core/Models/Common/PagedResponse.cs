using Core.ValueObjects;

namespace Core.Models.Common;

public record PagedResponse<T>
{
    public required PageNumber PageNumber { get; init; }
    public required PageSize PageSize { get; init; }
    public required PageCount PageCount { get; init; }
    public required TotalCount TotalCount { get; init; }
    public required HasMore HasMore { get; init; }
    public required List<T> Data { get; init; }
}