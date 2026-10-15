using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Approvals;

namespace Presentation.Validators.Approvals;

public class ApproveApprovalRequestValidator : AbstractValidator<ApproveApprovalRequestDto>
{
    public ApproveApprovalRequestValidator(IApprovalService approvalService)
    {
        RuleFor(x => x.ApprovalId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await approvalService.ExistsAsync(new OrganizationId(x.OrganizationId), new ApprovalId(x.ApprovalId), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Approval not found");

        RuleFor(x => x.Body!.Note)
            .MaximumLength(ApprovalNoteConstants.MaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Body?.Note));
    }
}

public class RejectApprovalRequestValidator : AbstractValidator<RejectApprovalRequestDto>
{
    public RejectApprovalRequestValidator(IApprovalService approvalService)
    {
        RuleFor(x => x.ApprovalId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await approvalService.ExistsAsync(new OrganizationId(x.OrganizationId), new ApprovalId(x.ApprovalId), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Approval not found");

        RuleFor(x => x.Body.Note)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("A note is required when rejecting a request")
            .MaximumLength(ApprovalNoteConstants.MaxLength);
    }
}

public class WithdrawApprovalRequestValidator : AbstractValidator<WithdrawApprovalRequestDto>
{
    public WithdrawApprovalRequestValidator(IApprovalService approvalService)
    {
        RuleFor(x => x.ApprovalId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await approvalService.ExistsAsync(new OrganizationId(x.OrganizationId), new ApprovalId(x.ApprovalId), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Approval not found");

        RuleFor(x => x.Body!.Note)
            .MaximumLength(ApprovalNoteConstants.MaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Body?.Note));
    }
}

internal static class ApprovalNoteConstants
{
    public const int MaxLength = 2048;
}
