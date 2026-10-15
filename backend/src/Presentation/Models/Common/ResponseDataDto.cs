namespace Presentation.Models.Common;

public record ResponseDataDto<T>
{
    public required T Data { get; init; }
}
