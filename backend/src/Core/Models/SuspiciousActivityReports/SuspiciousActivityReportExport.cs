using Core.ValueObjects;

namespace Core.Models.SuspiciousActivityReports;

public record SuspiciousActivityReportExport
{
    public required FileName FileName { get; init; }
    public required ContentType ContentType { get; init; }
    public required byte[] Content { get; init; }
}
