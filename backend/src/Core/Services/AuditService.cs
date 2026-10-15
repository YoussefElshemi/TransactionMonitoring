using System.Net;
using Core.Configs;
using Core.Documents;
using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models.AuditLogs;
using Core.Models.Common;
using Core.ValueObjects;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Options;

namespace Core.Services;

public class AuditService(
    IAuditLogRepository auditLogRepository,
    IAuditAnchorRepository auditAnchorRepository,
    IUnitOfWork unitOfWork,
    IOptions<AppConfig> appConfig,
    TimeProvider timeProvider) : IAuditService
{
    public Task RecordRevealAsync(RecordRevealRequest recordRevealRequest, CancellationToken stoppingToken)
    {
        return auditLogRepository.RecordRevealAsync(recordRevealRequest, stoppingToken);
    }

    public Task<PagedResponse<AuditLog>> ListAsync(ListAuditLogRequest listAuditLogRequest, CancellationToken stoppingToken)
    {
        return auditLogRepository.ListAsync(listAuditLogRequest, stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListAuditLogRequest listAuditLogRequest, CancellationToken stoppingToken)
    {
        return auditLogRepository.GetCountAsync(listAuditLogRequest, stoppingToken);
    }

    public async Task<AuditLogExport> ExportAsync(ExportAuditLogRequest exportAuditLogRequest, CancellationToken stoppingToken)
    {
        var entryCount = await auditLogRepository.CountEntriesAsync(exportAuditLogRequest, stoppingToken);
        var limit = appConfig.Value.AuditConfig.ExportEntryLimit;

        if (entryCount > limit)
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(ExportAuditLogRequest.FromDate),
                    $"This range holds {entryCount} entries and an export carries at most {limit}. Narrow the dates or name an entity.")
                {
                    ErrorCode = HttpStatusCode.Conflict.ToString()
                }
            ]);
        }

        var auditLogEntries = await auditLogRepository.ListEntriesAsync(exportAuditLogRequest, stoppingToken);

        return new AuditLogExport
        {
            FileName = new FileName($"audit-log-{timeProvider.GetUtcNow():yyyyMMdd'T'HHmmss'Z'}.csv"),
            ContentType = new ContentType(AuditLogCsvDocument.ContentType),
            Content = AuditLogCsvDocument.Write(auditLogEntries)
        };
    }

    public async Task<AuditChainVerification> VerifyAsync(
        VerifyAuditChainRequest verifyAuditChainRequest,
        CancellationToken stoppingToken)
    {
        var batchSize = new Count(appConfig.Value.AuditConfig.VerificationBatchSize);

        var entryCount = 0;
        Sequence? cursor = null;
        Hash? expectedPreviousHash = null;
        var seeded = false;

        AuditLogId? firstBrokenAuditLogId = null;
        Sequence? firstBrokenSequence = null;

        while (firstBrokenAuditLogId is null)
        {
            var batch = await auditLogRepository.ListChainAsync(
                verifyAuditChainRequest.OrganizationId, cursor, batchSize, verifyAuditChainRequest, stoppingToken);

            if (batch.Count == 0)
            {
                break;
            }

            cursor = batch[^1].Sequence;

            foreach (var auditLogEntry in batch)
            {
                if (!seeded)
                {
                    var previous = await auditLogRepository.FindEntryBeforeAsync(
                        verifyAuditChainRequest.OrganizationId, auditLogEntry.Sequence, stoppingToken);

                    expectedPreviousHash = previous?.Hash;
                    seeded = true;
                }

                entryCount++;

                if (AuditChainHasher.Compute(auditLogEntry) != auditLogEntry.Hash ||
                    auditLogEntry.PreviousHash != expectedPreviousHash)
                {
                    firstBrokenAuditLogId = auditLogEntry.AuditLogId;
                    firstBrokenSequence = auditLogEntry.Sequence;

                    break;
                }

                expectedPreviousHash = auditLogEntry.Hash;
            }
        }

        var auditAnchors = await auditAnchorRepository.ListAsync(
            verifyAuditChainRequest.OrganizationId, verifyAuditChainRequest, stoppingToken);

        var firstBrokenAuditAnchorId = await FindBrokenAnchorAsync(
            verifyAuditChainRequest.OrganizationId, auditAnchors, stoppingToken);

        return new AuditChainVerification
        {
            OrganizationId = verifyAuditChainRequest.OrganizationId,
            EntryCount = new TotalCount(entryCount),
            AnchorCount = new TotalCount(auditAnchors.Count),
            IsIntact = new IsIntact(firstBrokenAuditLogId is null && firstBrokenAuditAnchorId is null),
            FirstBrokenAuditLogId = firstBrokenAuditLogId,
            FirstBrokenSequence = firstBrokenSequence,
            FirstBrokenAuditAnchorId = firstBrokenAuditAnchorId,
            VerifiedAt = new VerifiedAt(timeProvider.GetUtcNow())
        };
    }

    public async Task<Count> AnchorAsync(CancellationToken stoppingToken)
    {
        var chainHeads = await auditLogRepository.ListChainHeadsAsync(stoppingToken);
        var latestAnchors = await auditAnchorRepository.ListLatestAsync(stoppingToken);

        var anchoredSequences = latestAnchors
            .Where(x => x.OrganizationId.HasValue)
            .ToDictionary(x => x.OrganizationId!.Value, x => x.Sequence);

        var anchoredPlatformSequence = latestAnchors.Find(x => !x.OrganizationId.HasValue)?.Sequence;

        var masterKey = DataKeyWrapper.ReadMasterKey(appConfig.Value.PayloadProtectionConfig.MasterKey);
        var now = timeProvider.GetUtcNow();
        var written = 0;

        foreach (var chainHead in chainHeads)
        {
            var anchoredSequence = chainHead.OrganizationId.HasValue
                ? anchoredSequences.TryGetValue(chainHead.OrganizationId.Value, out var sequence) ? sequence : null
                : anchoredPlatformSequence;

            if (anchoredSequence == chainHead.Sequence)
            {
                continue;
            }

            var auditAnchorId = new AuditAnchorId(Guid.NewGuid());
            var createdAt = new CreatedAt(now);

            await auditAnchorRepository.AddAsync(new AuditAnchor
            {
                AuditAnchorId = auditAnchorId,
                OrganizationId = chainHead.OrganizationId,
                Sequence = chainHead.Sequence,
                Hash = chainHead.Hash,
                Signature = AuditAnchorSigner.Sign(
                    masterKey, auditAnchorId, chainHead.OrganizationId, chainHead.Sequence, chainHead.Hash, createdAt),
                CreatedAt = createdAt
            }, stoppingToken);

            written++;
        }

        if (written > 0)
        {
            await unitOfWork.SaveChangesAsync(stoppingToken);
        }

        return new Count(written);
    }

    private async Task<AuditAnchorId?> FindBrokenAnchorAsync(
        OrganizationId organizationId,
        List<AuditAnchor> auditAnchors,
        CancellationToken stoppingToken)
    {
        if (auditAnchors.Count == 0)
        {
            return null;
        }

        var masterKey = DataKeyWrapper.ReadMasterKey(appConfig.Value.PayloadProtectionConfig.MasterKey);

        var hashes = await auditLogRepository.ListHashesAtAsync(
            organizationId, auditAnchors.ConvertAll(x => x.Sequence), stoppingToken);

        foreach (var auditAnchor in auditAnchors)
        {
            if (!AuditAnchorSigner.Verify(masterKey, auditAnchor) ||
                !hashes.TryGetValue(auditAnchor.Sequence, out var hash) ||
                hash != auditAnchor.Hash)
            {
                return auditAnchor.AuditAnchorId;
            }
        }

        return null;
    }
}
