namespace Presentation.Models.Common;

public record PagedResponseDto<T>
{
    public required PagedMetadataDto Metadata { get; init; }
    public required T[] Data { get; init; }
}