using System.Text;
using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.Attachments;
using Core.ValueObjects;

namespace Infrastructure.Services;

public class SignatureVirusScanner(IAttachmentStore attachmentStore) : IVirusScanner
{
    private const string EicarSignature =
        @"X5O!P%@AP[4\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*";

    public async Task<ScanResult> ScanAsync(StorageKey storageKey, CancellationToken stoppingToken)
    {
        await using var content = await attachmentStore.ReadAsync(storageKey, stoppingToken);
        using var reader = new StreamReader(content, Encoding.ASCII);

        var text = await reader.ReadToEndAsync(stoppingToken);

        if (!text.Contains(EicarSignature, StringComparison.Ordinal))
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
            ThreatName = new ThreatName("Eicar-Test-Signature")
        };
    }
}
