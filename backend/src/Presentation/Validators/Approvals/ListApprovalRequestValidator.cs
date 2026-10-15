using Core.Enums;
using FluentValidation;
using Presentation.Models.Approvals;

namespace Presentation.Validators.Approvals;

public class ListApprovalRequestValidator : AbstractValidator<ListApprovalRequestDto>
{
    public ListApprovalRequestValidator()
    {
        RuleFor(x => x.Status)
            .IsEnumName(typeof(ApprovalStatus), false)
            .When(x => !string.IsNullOrWhiteSpace(x.Status));

        RuleFor(x => x.Subject)
            .IsEnumName(typeof(ApprovalSubject), false)
            .When(x => !string.IsNullOrWhiteSpace(x.Subject));
    }
}
