using System.Net;
using Core.Configs;
using Core.Constants;
using Core.Documents;
using Core.Enums;
using Core.Exceptions;
using Core.Extensions;
using Core.Helpers;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Mappers.Webhooks;
using Core.Models.Approvals;
using Core.Models.CaseNotes;
using Core.Models.Common;
using Core.Models.SuspiciousActivityReports;
using Core.ValueObjects;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Options;

namespace Core.Services;

public class SuspiciousActivityReportService(
    ISuspiciousActivityReportRepository suspiciousActivityReportRepository,
    ISuspiciousActivityReportPolicyRepository suspiciousActivityReportPolicyRepository,
    IApprovalRepository approvalRepository,
    ICaseNoteRepository caseNoteRepository,
    IWebhookConfigurationService webhookConfigurationService,
    IOutboxMessageService outboxMessageService,
    IUnitOfWork unitOfWork,
    IOptions<AppConfig> appConfig,
    TimeProvider timeProvider,
    IModeContext modeContext) : ISuspiciousActivityReportService
{
    private const string GoAmlContentType = "application/xml";

    private const string SuspiciousActivityReportIdProblemMember = "suspiciousActivityReportId";

    public async Task<SuspiciousActivityReport> OpenAsync(
        OpenSuspiciousActivityReportRequest openSuspiciousActivityReportRequest,
        CancellationToken stoppingToken)
    {
        await EnforceNoExistingReportAsync(openSuspiciousActivityReportRequest.CaseId, stoppingToken);

        var suspiciousActivityReportPolicy = await suspiciousActivityReportPolicyRepository.FindAsync(
            openSuspiciousActivityReportRequest.OrganizationId, stoppingToken);

        var narrativeSource = await GetNarrativeSourceAsync(openSuspiciousActivityReportRequest.CaseId, stoppingToken);

        var narrative = openSuspiciousActivityReportRequest.Narrative ??
                        new Narrative(NarrativeComposer.Compose(narrativeSource, timeProvider.GetUtcNow()));

        var dueAt = FilingDeadlineResolver.Resolve(
            suspiciousActivityReportPolicy,
            appConfig.Value.SuspiciousActivityReportConfig,
            narrativeSource.Case.CreatedAt);

        var friendlyName = await suspiciousActivityReportRepository.GenerateFriendlyName(stoppingToken);

        var suspiciousActivityReport = openSuspiciousActivityReportRequest.ToModel(friendlyName, narrative, dueAt, timeProvider, modeContext.CurrentOrLive);

        await suspiciousActivityReportRepository.AddAsync(suspiciousActivityReport, stoppingToken);

        await StageWebhook(WebhookEvents.SuspiciousActivityReportOpened, suspiciousActivityReport, stoppingToken);

        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await GetAsync(suspiciousActivityReport.SuspiciousActivityReportId, stoppingToken);
    }

    public Task<SuspiciousActivityReport> GetAsync(SuspiciousActivityReportId suspiciousActivityReportId, CancellationToken stoppingToken)
    {
        return suspiciousActivityReportRepository.GetAsync(suspiciousActivityReportId, stoppingToken);
    }

    public Task<List<SuspiciousActivityReport>> ListByIdsAsync(
        IReadOnlyCollection<SuspiciousActivityReportId> suspiciousActivityReportIds,
        CancellationToken stoppingToken)
    {
        return suspiciousActivityReportRepository.ListByIdsAsync(suspiciousActivityReportIds, stoppingToken);
    }

    public Task<Exists> ExistsAsync(
        OrganizationId organizationId,
        SuspiciousActivityReportId suspiciousActivityReportId,
        CancellationToken stoppingToken)
    {
        return suspiciousActivityReportRepository.ExistsAsync(organizationId, suspiciousActivityReportId, stoppingToken);
    }

    public Task<PagedResponse<SuspiciousActivityReport>> ListAsync(
        ListSuspiciousActivityReportRequest listSuspiciousActivityReportRequest,
        CancellationToken stoppingToken)
    {
        return suspiciousActivityReportRepository.ListAsync(listSuspiciousActivityReportRequest, stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(
        ListSuspiciousActivityReportRequest listSuspiciousActivityReportRequest,
        CancellationToken stoppingToken)
    {
        return suspiciousActivityReportRepository.GetCountAsync(listSuspiciousActivityReportRequest, stoppingToken);
    }

    public Task<SuspiciousActivityReport?> FindByCaseAsync(CaseId caseId, CancellationToken stoppingToken)
    {
        return suspiciousActivityReportRepository.FindByCaseAsync(caseId, stoppingToken);
    }

    public async Task<SuspiciousActivityReport> UpdateNarrativeAsync(
        UpdateSuspiciousActivityReportRequest updateSuspiciousActivityReportRequest,
        CancellationToken stoppingToken)
    {
        var suspiciousActivityReport = await suspiciousActivityReportRepository.GetAsync(
            updateSuspiciousActivityReportRequest.SuspiciousActivityReportId, stoppingToken);

        EnforceEditable(suspiciousActivityReport);

        await suspiciousActivityReportRepository.UpdateNarrativeAsync(updateSuspiciousActivityReportRequest, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await GetAsync(updateSuspiciousActivityReportRequest.SuspiciousActivityReportId, stoppingToken);
    }

    public async Task<SuspiciousActivityReport> RecomposeNarrativeAsync(
        SuspiciousActivityReportId suspiciousActivityReportId,
        CancellationToken stoppingToken)
    {
        var suspiciousActivityReport = await suspiciousActivityReportRepository.GetAsync(suspiciousActivityReportId, stoppingToken);

        EnforceEditable(suspiciousActivityReport);

        var narrativeSource = await GetNarrativeSourceAsync(suspiciousActivityReport.CaseId, stoppingToken);

        await suspiciousActivityReportRepository.UpdateNarrativeAsync(new UpdateSuspiciousActivityReportRequest
        {
            SuspiciousActivityReportId = suspiciousActivityReportId,
            Narrative = new Narrative(NarrativeComposer.Compose(narrativeSource, timeProvider.GetUtcNow()))
        }, stoppingToken);

        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await GetAsync(suspiciousActivityReportId, stoppingToken);
    }

    public async Task<Approval?> SubmitAsync(
        SubmitSuspiciousActivityReportRequest submitSuspiciousActivityReportRequest,
        CancellationToken stoppingToken)
    {
        var suspiciousActivityReport = await suspiciousActivityReportRepository.GetAsync(
            submitSuspiciousActivityReportRequest.SuspiciousActivityReportId, stoppingToken);

        EnforceStatus(suspiciousActivityReport, SuspiciousActivityReportStatus.Draft);

        var suspiciousActivityReportPolicy = await suspiciousActivityReportPolicyRepository.FindAsync(
            suspiciousActivityReport.OrganizationId, stoppingToken);

        if (!FilingDeadlineResolver.RequiresFilingApproval(suspiciousActivityReportPolicy))
        {
            await TransitionAsync(suspiciousActivityReport, SuspiciousActivityReportStatus.Approved, stoppingToken);
            await unitOfWork.SaveChangesAsync(stoppingToken);

            return null;
        }

        var pending = await approvalRepository.FindPendingBySuspiciousActivityReportAsync(
            suspiciousActivityReport.SuspiciousActivityReportId, stoppingToken);

        if (pending is not null)
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(SuspiciousActivityReport.SuspiciousActivityReportId), "This report is already awaiting approval")
                {
                    ErrorCode = HttpStatusCode.Conflict.ToString()
                }
            ]);
        }

        return await RaiseApprovalAsync(suspiciousActivityReport, submitSuspiciousActivityReportRequest, stoppingToken);
    }

    public async Task ApplyApprovedSubmissionAsync(SuspiciousActivityReportId suspiciousActivityReportId, CancellationToken stoppingToken)
    {
        var suspiciousActivityReport = await suspiciousActivityReportRepository.GetAsync(suspiciousActivityReportId, stoppingToken);

        await TransitionAsync(suspiciousActivityReport, SuspiciousActivityReportStatus.Approved, stoppingToken);
    }

    public async Task ApplyRejectedSubmissionAsync(SuspiciousActivityReportId suspiciousActivityReportId, CancellationToken stoppingToken)
    {
        var suspiciousActivityReport = await suspiciousActivityReportRepository.GetAsync(suspiciousActivityReportId, stoppingToken);

        await TransitionAsync(suspiciousActivityReport, SuspiciousActivityReportStatus.Draft, stoppingToken);
    }

    public async Task<SuspiciousActivityReport> FileAsync(
        FileSuspiciousActivityReportRequest fileSuspiciousActivityReportRequest,
        CancellationToken stoppingToken)
    {
        if (modeContext.CurrentOrLive == Mode.Test)
        {
            throw new NotAvailableInTestModeException("Filing a suspicious activity report");
        }

        var suspiciousActivityReport = await suspiciousActivityReportRepository.GetAsync(
            fileSuspiciousActivityReportRequest.SuspiciousActivityReportId, stoppingToken);

        EnforceStatus(suspiciousActivityReport, SuspiciousActivityReportStatus.Approved);

        var filed = await suspiciousActivityReportRepository.TransitionAsync(new SuspiciousActivityReportTransition
        {
            SuspiciousActivityReportId = fileSuspiciousActivityReportRequest.SuspiciousActivityReportId,
            Status = SuspiciousActivityReportStatus.Filed,
            FiledByUserId = fileSuspiciousActivityReportRequest.FiledByUserId,
            FilingReference = fileSuspiciousActivityReportRequest.FilingReference,
            AcknowledgementReference = null,
            NotFiledReason = null,
            DecidedByUserId = null
        }, stoppingToken);

        await StageWebhook(WebhookEvents.SuspiciousActivityReportFiled, filed, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await GetAsync(fileSuspiciousActivityReportRequest.SuspiciousActivityReportId, stoppingToken);
    }

    public async Task<SuspiciousActivityReport> AcknowledgeAsync(
        AcknowledgeSuspiciousActivityReportRequest acknowledgeSuspiciousActivityReportRequest,
        CancellationToken stoppingToken)
    {
        var suspiciousActivityReport = await suspiciousActivityReportRepository.GetAsync(
            acknowledgeSuspiciousActivityReportRequest.SuspiciousActivityReportId, stoppingToken);

        EnforceStatus(suspiciousActivityReport, SuspiciousActivityReportStatus.Filed);

        var acknowledged = await suspiciousActivityReportRepository.TransitionAsync(new SuspiciousActivityReportTransition
        {
            SuspiciousActivityReportId = acknowledgeSuspiciousActivityReportRequest.SuspiciousActivityReportId,
            Status = SuspiciousActivityReportStatus.Acknowledged,
            FiledByUserId = null,
            FilingReference = null,
            AcknowledgementReference = acknowledgeSuspiciousActivityReportRequest.AcknowledgementReference,
            NotFiledReason = null,
            DecidedByUserId = null
        }, stoppingToken);

        await StageWebhook(WebhookEvents.SuspiciousActivityReportAcknowledged, acknowledged, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await GetAsync(acknowledgeSuspiciousActivityReportRequest.SuspiciousActivityReportId, stoppingToken);
    }

    public async Task<SuspiciousActivityReport> DeclineAsync(
        DeclineSuspiciousActivityReportRequest declineSuspiciousActivityReportRequest,
        CancellationToken stoppingToken)
    {
        var suspiciousActivityReport = await suspiciousActivityReportRepository.GetAsync(
            declineSuspiciousActivityReportRequest.SuspiciousActivityReportId, stoppingToken);

        EnforceStatus(suspiciousActivityReport, SuspiciousActivityReportStatus.Draft, SuspiciousActivityReportStatus.Approved);

        var declined = await suspiciousActivityReportRepository.TransitionAsync(new SuspiciousActivityReportTransition
        {
            SuspiciousActivityReportId = declineSuspiciousActivityReportRequest.SuspiciousActivityReportId,
            Status = SuspiciousActivityReportStatus.NotFiled,
            FiledByUserId = null,
            FilingReference = null,
            AcknowledgementReference = null,
            NotFiledReason = declineSuspiciousActivityReportRequest.Reason,
            DecidedByUserId = declineSuspiciousActivityReportRequest.DecidedByUserId
        }, stoppingToken);

        var createCaseNoteRequest = new CreateCaseNoteRequest
        {
            CaseId = suspiciousActivityReport.CaseId,
            OrganizationId = suspiciousActivityReport.OrganizationId,
            Message = declineSuspiciousActivityReportRequest.Reason,
            CreatedByUserId = declineSuspiciousActivityReportRequest.DecidedByUserId
        };

        await caseNoteRepository.AddAsync(createCaseNoteRequest.ToModel(timeProvider, modeContext.CurrentOrLive), stoppingToken);

        await StageWebhook(WebhookEvents.SuspiciousActivityReportNotFiled, declined, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);

        return await GetAsync(declineSuspiciousActivityReportRequest.SuspiciousActivityReportId, stoppingToken);
    }

    public async Task<SuspiciousActivityReportExport> ExportAsync(
        SuspiciousActivityReportId suspiciousActivityReportId,
        CancellationToken stoppingToken)
    {
        var suspiciousActivityReport = await suspiciousActivityReportRepository.GetAsync(suspiciousActivityReportId, stoppingToken);

        var suspiciousActivityReportPolicy = await suspiciousActivityReportPolicyRepository.FindAsync(
            suspiciousActivityReport.OrganizationId, stoppingToken);

        if (suspiciousActivityReportPolicy?.ReportingEntityId is null)
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(SuspiciousActivityReportPolicy.ReportingEntityId),
                    "This organization has not declared a reporting entity id, so a goAML export cannot be built")
                {
                    ErrorCode = HttpStatusCode.Conflict.ToString()
                }
            ]);
        }

        var narrativeSource = await GetNarrativeSourceAsync(suspiciousActivityReport.CaseId, stoppingToken);

        var content = GoAmlReportDocument.Write(
            suspiciousActivityReport, suspiciousActivityReportPolicy, narrativeSource, timeProvider.GetUtcNow());

        return new SuspiciousActivityReportExport
        {
            FileName = new FileName($"{suspiciousActivityReport.FriendlyName}.xml"),
            ContentType = new ContentType(GoAmlContentType),
            Content = content
        };
    }

    public Task<SuspiciousActivityReportPolicy?> FindPolicyAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        return suspiciousActivityReportPolicyRepository.FindAsync(organizationId, stoppingToken);
    }

    public async Task SetPolicyAsync(
        SetSuspiciousActivityReportPolicyRequest setSuspiciousActivityReportPolicyRequest,
        CancellationToken stoppingToken)
    {
        await suspiciousActivityReportPolicyRepository.SetAsync(setSuspiciousActivityReportPolicyRequest, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    private Task<NarrativeSource> GetNarrativeSourceAsync(CaseId caseId, CancellationToken stoppingToken)
    {
        return suspiciousActivityReportRepository.GetNarrativeSourceAsync(
            caseId, new Count(appConfig.Value.SuspiciousActivityReportConfig.NarrativeNoteLimit), stoppingToken);
    }

    private Task<SuspiciousActivityReport> TransitionAsync(
        SuspiciousActivityReport suspiciousActivityReport,
        SuspiciousActivityReportStatus status,
        CancellationToken stoppingToken)
    {
        return suspiciousActivityReportRepository.TransitionAsync(new SuspiciousActivityReportTransition
        {
            SuspiciousActivityReportId = suspiciousActivityReport.SuspiciousActivityReportId,
            Status = status,
            FiledByUserId = null,
            FilingReference = null,
            AcknowledgementReference = null,
            NotFiledReason = null,
            DecidedByUserId = null
        }, stoppingToken);
    }

    private async Task<Approval> RaiseApprovalAsync(
        SuspiciousActivityReport suspiciousActivityReport,
        SubmitSuspiciousActivityReportRequest submitSuspiciousActivityReportRequest,
        CancellationToken stoppingToken)
    {
        await TransitionAsync(suspiciousActivityReport, SuspiciousActivityReportStatus.PendingApproval, stoppingToken);

        var createApprovalRequest = new CreateApprovalRequest
        {
            OrganizationId = suspiciousActivityReport.OrganizationId,
            Subject = ApprovalSubject.SuspiciousActivityReport,
            RuleDeploymentId = null,
            CaseId = null,
            SuspiciousActivityReportId = suspiciousActivityReport.SuspiciousActivityReportId,
            ProposedCaseResolution = null,
            Reason = submitSuspiciousActivityReportRequest.Reason,
            RequestedByUserId = submitSuspiciousActivityReportRequest.SubmittedByUserId
        };

        var approval = createApprovalRequest.ToModel(timeProvider, modeContext.CurrentOrLive);

        await approvalRepository.AddAsync(approval, stoppingToken);

        var webhookConfigurations = await webhookConfigurationService.GetAllAsync(suspiciousActivityReport.OrganizationId, stoppingToken);

        await outboxMessageService.StageManyAsync(
            webhookConfigurations.ToOutboxMessageRequests(WebhookEvents.ApprovalRequested, approval.ToWebhook(), timeProvider.GetUtcNow()),
            stoppingToken);

        await unitOfWork.SaveChangesAsync(stoppingToken);

        return approval;
    }

    private async Task EnforceNoExistingReportAsync(CaseId caseId, CancellationToken stoppingToken)
    {
        var existing = await suspiciousActivityReportRepository.FindByCaseAsync(caseId, stoppingToken);

        if (existing is null)
        {
            return;
        }

        var message = existing.Status == SuspiciousActivityReportStatus.NotFiled
            ? "This case was already considered and recorded as not filed"
            : $"This case already has a report, which is {existing.Status.ToString().ToLowerInvariant()}";

        throw new ValidationException(
        [
            new ValidationFailure(nameof(SuspiciousActivityReport.CaseId), message)
            {
                ErrorCode = HttpStatusCode.Conflict.ToString(),

                CustomState = new Dictionary<string, object>
                {
                    [SuspiciousActivityReportIdProblemMember] = (Guid)existing.SuspiciousActivityReportId
                }
            }
        ]);
    }

    private static void EnforceEditable(SuspiciousActivityReport suspiciousActivityReport)
    {
        if (suspiciousActivityReport.Status.IsEditable())
        {
            return;
        }

        throw new ValidationException(
        [
            new ValidationFailure(nameof(SuspiciousActivityReport.Narrative),
                $"This report is {suspiciousActivityReport.Status.ToString().ToLowerInvariant()} and its narrative can no longer be changed")
            {
                ErrorCode = HttpStatusCode.Conflict.ToString()
            }
        ]);
    }

    private static void EnforceStatus(SuspiciousActivityReport suspiciousActivityReport, params SuspiciousActivityReportStatus[] allowed)
    {
        if (allowed.Contains(suspiciousActivityReport.Status))
        {
            return;
        }

        throw new ValidationException(
        [
            new ValidationFailure(nameof(SuspiciousActivityReport.Status),
                $"This report is {suspiciousActivityReport.Status.ToString().ToLowerInvariant()} and cannot be moved from there")
            {
                ErrorCode = HttpStatusCode.Conflict.ToString()
            }
        ]);
    }

    private async Task StageWebhook(string @event, SuspiciousActivityReport suspiciousActivityReport, CancellationToken stoppingToken)
    {
        var webhookConfigurations = await webhookConfigurationService.GetAllAsync(suspiciousActivityReport.OrganizationId, stoppingToken);

        await outboxMessageService.StageManyAsync(
            webhookConfigurations.ToOutboxMessageRequests(@event, suspiciousActivityReport.ToWebhook(), timeProvider.GetUtcNow()),
            stoppingToken);
    }
}
