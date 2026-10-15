using Core.Models.Attachments;
using Core.ValueObjects;

namespace Core.Interfaces.Services;

public interface IVirusScanner
{
    Task<ScanResult> ScanAsync(StorageKey storageKey, CancellationToken stoppingToken);
}
