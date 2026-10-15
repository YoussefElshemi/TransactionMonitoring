namespace Core.Models.Revenue;

public record ExportedRevenueRecognition
{
    public required List<RecognitionEntry> Entries { get; init; }
    public required byte[] Content { get; init; }
}
