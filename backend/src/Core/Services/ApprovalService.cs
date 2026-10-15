using System.Net;
using Core.Constants;
using Core.Enums;
using Core.Interfaces;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Mappers.Webhooks;
using Core.Models.Approvals;
using Core.Models.CaseNotes;
using Core.Models.Cases;
using Core.Models.Common;
using Core.ValueObjects;
using FluentValidation;
using FluentValidation.Results;

namespace Core.Services;

public class ApprovalService(
    IApprovalRepository approvalRepository,
    IApprovalPolicyRepository approvalPolicyRepository,
    ICaseService caseService,
    IRuleDeploymentService ruleDeploymentService,
    ISuspiciousActivityReportService suspiciousActivityReportService,
    ICaseNoteRepository caseNoteRepository,
    IWebhookConfigurationService webhookConfigurationService,
    IOutboxMessageService outboxMessageService,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider,
    IModeContext modeContext) : IApprovalService
{
    public Task<Approval> GetAsync(ApprovalId approvalId, CancellationToken stoppingToken)
    {
        return approvalRepository.GetAsync(approvalId, stoppingToken);
    }

    public Task<Exists> ExistsAsync(OrganizationId organizationId, ApprovalId approvalId, CancellationToken stoppingToken)
    {
        return approvalRepository.ExistsAsync(organizationId, approvalId, stoppingToken);
    }

    public Task<PagedResponse<Approval>> ListAsync(ListApprovalRequest listApprovalRequest, CancellationToken stoppingToken)
    {
        return approvalRepository.ListAsync(listApprovalRequest, stoppingToken);
    }

    public Task<TotalCount> GetCountAsync(ListApprovalRequest listApprovalRequest, CancellationToken stoppingToken)
    {
        return approvalRepository.GetCountAsync(listApprovalRequest, stoppingToken);
    }

    public async Task<Approval> ReviewAsync(ReviewApprovalRequest reviewApprovalRequest, CancellationToken stoppingToken)
    {
        var approval = await approvalRepository.GetAsync(reviewApprovalRequest.ApprovalId, stoppingToken);

        EnforcePending(approval);
        EnforceSeparation(approval, reviewApprovalRequest);

        var reviewed = await approvalRepository.ReviewAsync(reviewApprovalRequest, stoppingToken);

        await StageApprovalWebhook(reviewed, stoppingToken);

        await ApplyAsync(reviewed, stoppingToken);

        await unitOfWork.SaveChangesAsync(stoppingToken);

        return reviewed;
    }

    public Task<ApprovalPolicy?> FindPolicyAsync(OrganizationId organizationId, CancellationToken stoppingToken)
    {
        return approvalPolicyRepository.FindAsync(organizationId, stoppingToken);
    }

    public async Task SetPolicyAsync(SetApprovalPolicyRequest setApprovalPolicyRequest, CancellationToken stoppingToken)
    {
        await approvalPolicyRepository.SetAsync(setApprovalPolicyRequest, stoppingToken);
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    private static void EnforcePending(Approval approval)
    {
        if (approval.Status == ApprovalStatus.Pending)
        {
            return;
        }

        throw new ValidationException(
        [
            new ValidationFailure(nameof(Approval.ApprovalId), $"This request has already been {approval.Status.ToString().ToLowerInvariant()}")
            {
                ErrorCode = HttpStatusCode.Conflict.ToString()
            }
        ]);
    }

    private static void EnforceSeparation(Approval approval, ReviewApprovalRequest reviewApprovalRequest)
    {
        var isRequester = approval.RequestedByUserId == reviewApprovalRequest.ReviewedByUserId;

        if (reviewApprovalRequest.Status == ApprovalStatus.Withdrawn)
        {
            if (isRequester)
            {
                return;
            }

            throw new ValidationException(
            [
                new ValidationFailure(nameof(Approval.ApprovalId), "Only the user who raised this request may withdraw it")
                {
                    ErrorCode = HttpStatusCode.Forbidden.ToString()
                }
            ]);
        }

        if (!isRequester)
        {
            return;
        }

        throw new ValidationException(
        [
            new ValidationFailure(nameof(Approval.ApprovalId), "The user who raised this request may not review it")
            {
                ErrorCode = HttpStatusCode.Forbidden.ToString()
            }
        ]);
    }

    private Task ApplyAsync(Approval approval, CancellationToken stoppingToken)
    {
        return approval.Subject switch
        {
            ApprovalSubject.RuleDeployment => ApplyRuleDeploymentAsync(approval, stoppingToken),
            ApprovalSubject.CaseResolution => ApplyCaseResolutionAsync(approval, stoppingToken),
            ApprovalSubject.SuspiciousActivityReport => ApplySuspiciousActivityReportAsync(approval, stoppingToken),
            _ => Task.CompletedTask
        };
    }

    private async Task ApplyRuleDeploymentAsync(Approval approval, CancellationToken stoppingToken)
    {
        if (!approval.RuleDeploymentId.HasValue)
        {
            return;
        }

        if (approval.Status == ApprovalStatus.Approved)
        {
            await ruleDeploymentService.ExecuteApprovedAsync(approval.RuleDeploymentId.Value, stoppingToken);

            return;
        }

        await ruleDeploymentService.CancelDeploymentAsync(approval.RuleDeploymentId.Value, stoppingToken);
    }

    private async Task ApplySuspiciousActivityReportAsync(Approval approval, CancellationToken stoppingToken)
    {
        if (!approval.SuspiciousActivityReportId.HasValue)
        {
            return;
        }

        if (approval.Status == ApprovalStatus.Approved)
        {
            await suspiciousActivityReportService.ApplyApprovedSubmissionAsync(approval.SuspiciousActivityReportId.Value, stoppingToken);

            return;
        }

        await suspiciousActivityReportService.ApplyRejectedSubmissionAsync(approval.SuspiciousActivityReportId.Value, stoppingToken);
    }

    private async Task ApplyCaseResolutionAsync(Approval approval, CancellationToken stoppingToken)
    {
        if (!approval.CaseId.HasValue || approval.ProposedCaseResolution is null)
        {
            return;
        }

        if (approval.Status == ApprovalStatus.Approved)
        {
            await caseService.ApplyApprovedResolutionAsync(new ResolveCaseRequest
            {
                CaseId = approval.CaseId.Value,
                OrganizationId = approval.OrganizationId,
                Status = approval.ProposedCaseResolution.Status,
                Disposition = approval.ProposedCaseResolution.Disposition,
                CaseReasonCodeId = approval.ProposedCaseResolution.CaseReasonCodeId,
                Note = approval.ProposedCaseResolution.Note,

                ResolvedByUserId = approval.RequestedByUserId
            }, stoppingToken);

            return;
        }

        if (approval.Status == ApprovalStatus.Rejected && approval.ReviewNote.HasValue && approval.ReviewedByUserId.HasValue)
        {
            var createCaseNoteRequest = new CreateCaseNoteRequest
            {
                CaseId = approval.CaseId.Value,
                OrganizationId = approval.OrganizationId,
                Message = approval.ReviewNote.Value,
                CreatedByUserId = approval.ReviewedByUserId.Value
            };

            await caseNoteRepository.AddAsync(createCaseNoteRequest.ToModel(timeProvider, modeContext.CurrentOrLive), stoppingToken);
        }
    }

    private async Task StageApprovalWebhook(Approval approval, CancellationToken stoppingToken)
    {
        var @event = approval.Status switch
        {
            ApprovalStatus.Approved => WebhookEvents.ApprovalApproved,
            ApprovalStatus.Rejected => WebhookEvents.ApprovalRejected,
            ApprovalStatus.Withdrawn => WebhookEvents.ApprovalWithdrawn,
            _ => WebhookEvents.ApprovalRequested
        };

        var webhookConfigurations = await webhookConfigurationService.GetAllAsync(approval.OrganizationId, stoppingToken);

        await outboxMessageService.StageManyAsync(
            webhookConfigurations.ToOutboxMessageRequests(@event, approval.ToWebhook(), timeProvider.GetUtcNow()), stoppingToken);
    }
}
