using System.Net;
using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using FluentValidation.Results;
using Presentation.Models.Users;

namespace Presentation.Validators.Users;

public class RemoveUserRoleRequestValidator : AbstractValidator<RemoveUserRoleRequestDto>
{
    private readonly IApplicationUserService _applicationUserService;

    public RemoveUserRoleRequestValidator(IApplicationUserService applicationUserService)
    {
        _applicationUserService = applicationUserService;

        RuleFor(x => x.UserId)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MustAsync(async (x, ct) => await applicationUserService.ExistsAsync(new ApplicationUserId(x), new IncludeDeleted(false), ct))
            .WithErrorCode(HttpStatusCode.NotFound.ToString())
            .WithMessage("User not found")
            .DependentRules(() =>
            {
                RuleFor(x => x)
                    .CustomAsync(CanModifyThisUser);
            });

        RuleFor(x => x.Role)
            .IsEnumName(typeof(UserRole), false);
    }

    private async Task CanModifyThisUser(RemoveUserRoleRequestDto removeUserRoleRequestDto, ValidationContext<RemoveUserRoleRequestDto> context, CancellationToken stoppingToken)
    {
        var user = await _applicationUserService.GetAsync(new ApplicationUserId(removeUserRoleRequestDto.UserId), stoppingToken);

        if (Enum.TryParse<UserRole>(removeUserRoleRequestDto.Role, true, out var role))
        {
            if (user.Roles.Contains(UserRole.Admin) && role == UserRole.Admin)
            {
                context.AddFailure(new ValidationFailure
                {
                    ErrorMessage = "You cannot remove the admin role from this user.",
                    ErrorCode = HttpStatusCode.Forbidden.ToString()
                });
            }

            if (!user.Roles.Contains(role))
            {
                context.AddFailure("The user does not have this role");
            }
        }
    }
}