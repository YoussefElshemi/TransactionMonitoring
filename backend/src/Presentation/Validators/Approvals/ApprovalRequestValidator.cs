using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Approvals;

namespace Presentation.Validators.Approvals;

public class ApprovalRequestValidator : AbstractValidator<ApprovalRequestDto>
{
    public ApprovalRequestValidator(IApprovalService approvalService)
    {
        RuleFor(x => x.ApprovalId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, _, ct) => await approvalService.ExistsAsync(new OrganizationId(x.OrganizationId), new ApprovalId(x.ApprovalId), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("Approval not found");
    }
}
