using System.Security.Cryptography;
using Core.Configs;
using Core.Interfaces.Services;
using Core.Models.Attachments;
using Core.ValueObjects;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public class FileSystemAttachmentStore(IOptions<AppConfig> appConfig) : IAttachmentStore
{
    public async Task<StoredAttachment> WriteAsync(
        OrganizationId organizationId, AttachmentId attachmentId, Stream content, CancellationToken stoppingToken)
    {
        var relativeKey = $"{organizationId}/{attachmentId}";
        var absolutePath = ResolvePath(new StorageKey(relativeKey));

        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);

        long byteCount;
        byte[] hash;

        await using (var fileStream = File.Create(absolutePath))
        {
            using var sha256 = SHA256.Create();
            await using var cryptoStream = new CryptoStream(fileStream, sha256, CryptoStreamMode.Write, true);

            await content.CopyToAsync(cryptoStream, stoppingToken);
            await cryptoStream.FlushFinalBlockAsync(stoppingToken);

            byteCount = fileStream.Length;
            hash = sha256.Hash!;
        }

        return new StoredAttachment
        {
            StorageKey = new StorageKey(relativeKey),
            ByteCount = new ByteCount(byteCount),
            Checksum = new Checksum(Convert.ToHexString(hash).ToLowerInvariant())
        };
    }

    public Task<Stream> ReadAsync(StorageKey storageKey, CancellationToken stoppingToken)
    {
        return Task.FromResult<Stream>(File.OpenRead(ResolvePath(storageKey)));
    }

    public Task DeleteAsync(StorageKey storageKey, CancellationToken stoppingToken)
    {
        var absolutePath = ResolvePath(storageKey);

        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }

        return Task.CompletedTask;
    }

    private string ResolvePath(StorageKey storageKey)
    {
        var rootPath = Path.GetFullPath(appConfig.Value.AttachmentConfig.RootPath);
        var absolutePath = Path.GetFullPath(Path.Combine(rootPath, storageKey));

        if (!absolutePath.StartsWith(rootPath, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Storage key resolves outside the attachment root: {storageKey}");
        }

        return absolutePath;
    }
}
