using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Extensions;
using Presentation.Factories;
using Presentation.Mappers.Common;
using Presentation.Mappers.Users;
using Presentation.Models.Common;
using Presentation.Models.Users;

namespace Presentation.Controllers;

[RequirePermission(Permission.UsersRead)]
[RequirePermission(Permission.PlatformAdminister)]
[SandboxMeterExempt]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[Route("users")]
public class UserController(
    IApplicationUserService applicationUserService,
    UserResponseFactory userResponseFactory,
    IValidator<UserRequestDto> userRequestValidator,
    IValidator<DeleteUserRequestDto> deleteUserRequestValidator,
    IValidator<AddUserRoleRequestDto> addUserRoleRequestValidator,
    IValidator<RemoveUserRoleRequestDto> removeUserRoleRequestValidator,
    IValidator<RecoverUserRequestDto> recoverUserRequestValidator,
    IValidator<ListUserRequestDto> listUserRequestValidator) : Controller
{
    [HttpGet]
    [ProducesResponseType<PagedResponseDto<UserResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListUsers([FromQuery] ListUserRequestDto listUserRequestDto)
    {
        await listUserRequestValidator.ValidateAndThrowAsync(listUserRequestDto, HttpContext.RequestAborted);

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var users = await applicationUserService.ListAsync(listUserRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(users.ToDto(userResponseFactory.Create(users.Data, context)));
    }

    [HttpGet("{UserId:guid}")]
    [ProducesResponseType<ResponseDataDto<UserResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUser(UserRequestDto userRequestDto)
    {
        await userRequestValidator.ValidateAndThrowAsync(userRequestDto, HttpContext.RequestAborted);

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var user = await applicationUserService.GetAsync(new ApplicationUserId(userRequestDto.UserId), HttpContext.RequestAborted);

        return Ok(userResponseFactory.Create(user, context).ToResponseData());
    }

    [RequirePermission(Permission.UsersWrite)]
    [RequirePermission(Permission.PlatformAdminister)]
    [Idempotent]
    [HttpDelete("{UserId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteUser(DeleteUserRequestDto deleteUserRequestDto)
    {
        await deleteUserRequestValidator.ValidateAndThrowAsync(deleteUserRequestDto, HttpContext.RequestAborted);

        await applicationUserService.DeleteAsync(new ApplicationUserId(deleteUserRequestDto.UserId), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.UsersWrite)]
    [RequirePermission(Permission.PlatformAdminister)]
    [Idempotent]
    [HttpPost("{UserId:guid}/recover")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RecoverUser(RecoverUserRequestDto recoverUserRequest)
    {
        await recoverUserRequestValidator.ValidateAndThrowAsync(recoverUserRequest, HttpContext.RequestAborted);

        await applicationUserService.RecoverAsync(new ApplicationUserId(recoverUserRequest.UserId), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.UsersWrite)]
    [RequirePermission(Permission.PlatformAdminister)]
    [Idempotent]
    [HttpPut("{UserId:guid}/roles/{Role}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AddUserRole(AddUserRoleRequestDto addUserRoleRequestDto)
    {
        await addUserRoleRequestValidator.ValidateAndThrowAsync(addUserRoleRequestDto, HttpContext.RequestAborted);

        var userRole = Enum.Parse<UserRole>(addUserRoleRequestDto.Role, true);
        await applicationUserService.AddRoleAsync(new ApplicationUserId(addUserRoleRequestDto.UserId), userRole, HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.UsersWrite)]
    [RequirePermission(Permission.PlatformAdminister)]
    [Idempotent]
    [HttpDelete("{UserId:guid}/roles/{Role}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveUserRole(RemoveUserRoleRequestDto removeUserRoleRequestDto)
    {
        await removeUserRoleRequestValidator.ValidateAndThrowAsync(removeUserRoleRequestDto, HttpContext.RequestAborted);

        var userRole = Enum.Parse<UserRole>(removeUserRoleRequestDto.Role, true);
        await applicationUserService.RemoveRoleAsync(new ApplicationUserId(removeUserRoleRequestDto.UserId), userRole, HttpContext.RequestAborted);

        return NoContent();
    }
}