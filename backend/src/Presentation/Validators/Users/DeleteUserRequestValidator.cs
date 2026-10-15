using System.Net;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Presentation.Models.Users;

namespace Presentation.Validators.Users;

public class DeleteUserRequestValidator : AbstractValidator<DeleteUserRequestDto>
{
    public DeleteUserRequestValidator(IApplicationUserService applicationUserService)
    {
        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await applicationUserService.ExistsAsync(new ApplicationUserId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("User not found");
    }
}