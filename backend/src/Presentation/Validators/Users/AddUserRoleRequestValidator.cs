using System.Net;
using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using FluentValidation.Results;
using Presentation.Models.Users;

namespace Presentation.Validators.Users;

public class AddUserRoleRequestValidator : AbstractValidator<AddUserRoleRequestDto>
{
    private readonly IApplicationUserService _applicationUserService;

    public AddUserRoleRequestValidator(IApplicationUserService applicationUserService)
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

    private async Task CanModifyThisUser(AddUserRoleRequestDto addUserRoleRequestDto, ValidationContext<AddUserRoleRequestDto> context, CancellationToken stoppingToken)
    {
        var user = await _applicationUserService.GetAsync(new ApplicationUserId(addUserRoleRequestDto.UserId), stoppingToken);

        if (user.Roles.Select(x => x.ToString()).Contains(addUserRoleRequestDto.Role, StringComparer.OrdinalIgnoreCase))
        {
            context.AddFailure(new ValidationFailure
            {
                ErrorMessage = "The user already has this role",
                ErrorCode = HttpStatusCode.Conflict.ToString()
            });
        }
    }
}