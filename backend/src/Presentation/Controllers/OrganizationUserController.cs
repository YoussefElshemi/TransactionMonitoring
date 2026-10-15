using Core.Constants;
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
using Presentation.Mappers.AuditLogs;
using Presentation.Mappers.Common;
using Presentation.Mappers.OrganizationUsers;
using Presentation.Models.AuditLogs;
using Presentation.Models.Common;
using Presentation.Models.OrganizationUsers;
using Presentation.Models.Users;

namespace Presentation.Controllers;

[RequirePermission(Permission.UsersRead)]
[SandboxMeterExempt]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[Route("organizations/{OrganizationId:guid}/users")]
public class OrganizationUserController(
    IApplicationUserService applicationUserService,
    IOrganizationUserService organizationUserService,
    IOffboardingService offboardingService,
    IAuditService auditService,
    UserResponseFactory userResponseFactory,
    AuditLogResponseFactory auditLogResponseFactory,
    IValidator<OrganizationUserRequestDto> organizationUserRequestValidator,
    IValidator<RemoveOrganizationUserRequestDto> removeOrganizationUserRequestValidator,
    IValidator<UpdateOrganizationUserRequestDto> updateOrganizationUserRequestValidator,
    IValidator<ListOrganizationUserRequestDto> listOrganizationUserRequestValidator,
    IValidator<ListMyOrganizationUserRequestDto> listMyOrganizationUserRequestValidator,
    IValidator<SetUserSubOrganizationRequestDto> setUserSubOrganizationRequestValidator,
    IValidator<OffboardUserRequestDto> offboardUserRequestValidator) : Controller
{
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [HttpGet]
    [ProducesResponseType<PagedResponseDto<UserResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListUsers([FromQuery] ListOrganizationUserRequestDto listOrganizationUserRequestDto)
    {
        await listOrganizationUserRequestValidator.ValidateAndThrowAsync(listOrganizationUserRequestDto, HttpContext.RequestAborted);

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var users = await organizationUserService.ListAsync(listOrganizationUserRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(users.ToDto(userResponseFactory.Create(users.Data, context)));
    }

    [HttpGet("/organizations/my/users")]
    [ProducesResponseType<PagedResponseDto<UserResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListMyUsers([FromQuery] ListMyOrganizationUserRequestDto listMyOrganizationUserRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listMyOrganizationUserRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await listMyOrganizationUserRequestValidator.ValidateAndThrowAsync(listMyOrganizationUserRequestDto, HttpContext.RequestAborted);

        var users = await organizationUserService.ListAsync(listMyOrganizationUserRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(users.ToDto(userResponseFactory.Create(users.Data, authenticatedUser.ToEnrichmentContext(User))));
    }

    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [HttpGet("{UserId:guid}")]
    [ProducesResponseType<ResponseDataDto<UserResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUser(OrganizationUserRequestDto organizationUserRequestDto)
    {
        await organizationUserRequestValidator.ValidateAndThrowAsync(organizationUserRequestDto, HttpContext.RequestAborted);

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var user = await applicationUserService.GetAsync(new ApplicationUserId(organizationUserRequestDto.UserId), HttpContext.RequestAborted);

        return Ok(userResponseFactory.Create(user, context).ToResponseData());
    }

    [RequirePermission(Permission.UsersWrite)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpPut("{UserId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> PutUser(UpdateOrganizationUserRequestDto updateOrganizationUserRequestDto)
    {
        await updateOrganizationUserRequestValidator.ValidateAndThrowAsync(updateOrganizationUserRequestDto, HttpContext.RequestAborted);

        var user = await applicationUserService.GetAsync(new ApplicationUserId(updateOrganizationUserRequestDto.UserId), HttpContext.RequestAborted);

        user.OrganizationId = new OrganizationId(updateOrganizationUserRequestDto.OrganizationId);

        await applicationUserService.UpdateAsync(user, HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.SubOrganizationsWrite)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpPut("{UserId:guid}/sub-organization")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetUserSubOrganization(SetUserSubOrganizationRequestDto setUserSubOrganizationRequestDto)
    {
        await setUserSubOrganizationRequestValidator.ValidateAndThrowAsync(setUserSubOrganizationRequestDto, HttpContext.RequestAborted);

        await applicationUserService.SetSubOrganizationAsync(
            new ApplicationUserId(setUserSubOrganizationRequestDto.UserId),
            setUserSubOrganizationRequestDto.Body.SubOrganizationId.HasValue
                ? new SubOrganizationId(setUserSubOrganizationRequestDto.Body.SubOrganizationId.Value)
                : null,
            HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.UsersOffboard)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpPost("{UserId:guid}/offboard")]
    [ProducesResponseType<ResponseDataDto<OffboardedUserResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> OffboardUser(OffboardUserRequestDto offboardUserRequestDto)
    {
        await offboardUserRequestValidator.ValidateAndThrowAsync(offboardUserRequestDto, HttpContext.RequestAborted);

        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        var offboardedUser = await offboardingService.OffboardAsync(
            offboardUserRequestDto.ToModel(authenticatedUser.UserId), HttpContext.RequestAborted);

        return Ok(offboardedUser.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.UsersWrite)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpDelete("{UserId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveUser(RemoveOrganizationUserRequestDto removeOrganizationUserRequestDto)
    {
        await removeOrganizationUserRequestValidator.ValidateAndThrowAsync(removeOrganizationUserRequestDto, HttpContext.RequestAborted);

        var user = await applicationUserService.GetAsync(new ApplicationUserId(removeOrganizationUserRequestDto.UserId), HttpContext.RequestAborted);

        user.OrganizationId = new OrganizationId(SystemConstants.DefaultOrganization);

        await applicationUserService.UpdateAsync(user, HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.UsersRead)]
    [RequirePermission(Permission.AuditLogsRead)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [HttpGet("{userId:guid}/history")]
    [ProducesResponseType<PagedResponseDto<AuditLogResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserHistory(
        Guid organizationId,
        Guid userId,
        [FromQuery] ListAuditLogRequestDto listAuditLogRequestDto)
    {
        listAuditLogRequestDto.OrganizationId = organizationId;

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var auditLogs = await auditService.ListAsync(
            listAuditLogRequestDto.ToModel(AuditableEntityType.ApplicationUser, userId), HttpContext.RequestAborted);

        var resources = await auditLogResponseFactory.CreateAsync(auditLogs.Data, context, HttpContext.RequestAborted);

        return Ok(auditLogs.ToDto(resources));
    }
}