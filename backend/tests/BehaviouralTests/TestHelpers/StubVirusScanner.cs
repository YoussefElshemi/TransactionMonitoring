using System.Text;
using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.Attachments;
using Core.ValueObjects;

namespace BehaviouralTests.TestHelpers;

// Real EICAR files get deleted by the host's antivirus, making scans fail depending on the machine. This marker is inert.
public class StubVirusScanner(IAttachmentStore attachmentStore) : IVirusScanner
{
    public const string InfectedMarker = "INFECTED-TEST-MARKER";

    public async Task<ScanResult> ScanAsync(StorageKey storageKey, CancellationToken stoppingToken)
    {
        await using var content = await attachmentStore.ReadAsync(storageKey, stoppingToken);
        using var reader = new StreamReader(content, Encoding.ASCII);

        var text = await reader.ReadToEndAsync(stoppingToken);

        if (!text.Contains(InfectedMarker, StringComparison.Ordinal))
        {
            return new ScanResult
            {
                ScanStatus = AttachmentScanStatus.Clean,
                ThreatName = null
            };
        }

        return new ScanResult
        {
            ScanStatus = AttachmentScanStatus.Infected,
            ThreatName = new ThreatName("Stub-Test-Signature")
        };
    }
}
