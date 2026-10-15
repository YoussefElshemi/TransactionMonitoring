using System.Net;
using Core.Enums;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Models.Attachments;
using Core.Models.Common;
using Core.ValueObjects;
using FluentValidation;
using FluentValidation.Results;
using Serilog;

namespace Core.Services;

public class AttachmentService(
    IAttachmentRepository attachmentRepository,
    IAttachmentStore attachmentStore,
    IVirusScanner virusScanner,
    IUnitOfWork unitOfWork,
    ILogger logger,
    TimeProvider timeProvider,
    IModeContext modeContext) : IAttachmentService
{
    public async Task<Attachment> CreateAsync(CreateAttachmentRequest createAttachmentRequest, CancellationToken stoppingToken)
    {
        var attachmentId = new AttachmentId(Guid.NewGuid());

        var storedAttachment = await attachmentStore.WriteAsync(
            createAttachmentRequest.OrganizationId, attachmentId, createAttachmentRequest.Content, stoppingToken);

        var attachment = createAttachmentRequest.ToModel(attachmentId, storedAttachment, timeProvider, modeContext.CurrentOrLive);

        await attachmentRepository.AddAsync(attachment, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await ScanAsync(attachment, stoppingToken);
    }

    public async Task<AttachmentContent> DownloadAsync(AttachmentId attachmentId, CancellationToken stoppingToken)
    {
        var attachment = await attachmentRepository.GetAsync(attachmentId, stoppingToken);

        EnforceClean(attachment);

        var content = await attachmentStore.ReadAsync(attachment.StorageKey, stoppingToken);

        return new AttachmentContent
        {
            Attachment = attachment,
            Content = content
        };
    }

    public async Task<Attachment> RescanAsync(AttachmentId attachmentId, CancellationToken stoppingToken)
    {
        var attachment = await attachmentRepository.GetAsync(attachmentId, stoppingToken);

        if (attachment.ScanStatus is AttachmentScanStatus.Clean or AttachmentScanStatus.Infected)
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(Attachment.ScanStatus), $"This attachment has already been scanned and came back {attachment.ScanStatus.ToString().ToLowerInvariant()}")
                {
                    ErrorCode = HttpStatusCode.Conflict.ToString()
                }
            ]);
        }

        return await ScanAsync(attachment, stoppingToken);
    }

    public Task<Exists> ExistsAsync(OrganizationId organizationId, CaseId caseId, AttachmentId attachmentId, IncludeDeleted includeDeleted, CancellationToken stoppingToken)
    {
        return attachmentRepository.ExistsAsync(organizationId, caseId, attachmentId, includeDeleted, stoppingToken);
    }

    public Task<PagedResponse<Attachment>> ListAsync(ListAttachmentRequest listAttachmentRequest, CancellationToken stoppingToken)
    {
        return attachmentRepository.ListAsync(listAttachmentRequest, stoppingToken);
    }

    public Task<Attachment> GetAsync(AttachmentId attachmentId, CancellationToken stoppingToken)
    {
        return attachmentRepository.GetAsync(attachmentId, stoppingToken);
    }

    public async Task DeleteAsync(AttachmentId attachmentId, CancellationToken stoppingToken)
    {
        await attachmentRepository.DeleteAsync(attachmentId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public async Task RecoverAsync(AttachmentId attachmentId, CancellationToken stoppingToken)
    {
        await attachmentRepository.RecoverAsync(attachmentId, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListAttachmentRequest listAttachmentRequest, CancellationToken stoppingToken)
    {
        return attachmentRepository.GetCountAsync(listAttachmentRequest, stoppingToken);
    }

    private async Task<Attachment> ScanAsync(Attachment attachment, CancellationToken stoppingToken)
    {
        ScanResult scanResult;

        try
        {
            scanResult = await virusScanner.ScanAsync(attachment.StorageKey, stoppingToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.Error(exception, "Virus scan failed for attachment {AttachmentId}: {error}", attachment.AttachmentId, exception.Message);

            scanResult = new ScanResult
            {
                ScanStatus = AttachmentScanStatus.Failed,
                ThreatName = null
            };
        }

        if (scanResult.ScanStatus == AttachmentScanStatus.Infected)
        {
            await attachmentStore.DeleteAsync(attachment.StorageKey, stoppingToken);
        }

        await attachmentRepository.RecordScanResultAsync(attachment.AttachmentId, scanResult, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await attachmentRepository.GetAsync(attachment.AttachmentId, stoppingToken);
    }

    private static void EnforceClean(Attachment attachment)
    {
        if (attachment.ScanStatus == AttachmentScanStatus.Clean)
        {
            return;
        }

        throw new ValidationException(
        [
            new ValidationFailure(nameof(Attachment.ScanStatus), $"This attachment cannot be downloaded while its scan status is {attachment.ScanStatus.ToString().ToLowerInvariant()}")
            {
                ErrorCode = HttpStatusCode.Conflict.ToString()
            }
        ]);
    }
}
