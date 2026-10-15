using Core.ValueObjects;

namespace Core.Models.Revenue;

public record ExportRevenueRecognitionRequest
{
    public required Month FromMonth { get; init; }
    public required Year FromYear { get; init; }
    public required Month ToMonth { get; init; }
    public required Year ToYear { get; init; }
}
