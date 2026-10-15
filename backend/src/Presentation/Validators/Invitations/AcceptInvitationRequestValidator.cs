using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Invitations;

namespace Presentation.Validators.Invitations;

public class AcceptInvitationRequestValidator : AbstractValidator<AcceptInvitationRequestDto>
{
    public AcceptInvitationRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .MaximumLength(InvitationToken.MaxLength);

        RuleFor(x => x.UserName)
            .MaximumLength(256)
            .When(x => !string.IsNullOrWhiteSpace(x.UserName));

        RuleFor(x => x.Password)
            .MaximumLength(Password.MaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Password));
    }
}

public class PreviewInvitationRequestValidator : AbstractValidator<PreviewInvitationRequestDto>
{
    public PreviewInvitationRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .MaximumLength(InvitationToken.MaxLength);
    }
}
