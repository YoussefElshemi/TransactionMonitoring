namespace Presentation.Models.Usages;

public class UsageResponseDto
{
    public required Guid UsageId { get; init; }
    public required string Resource { get; init; }
    public required string BillableResource { get; init; }
    public required int Count { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }

    public required string Mode { get; init; }
}