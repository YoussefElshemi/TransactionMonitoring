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
using Presentation.Mappers.Roles;
using Presentation.Models.AuditLogs;
using Presentation.Models.Common;
using Presentation.Models.Roles;

namespace Presentation.Controllers;

[SandboxMeterExempt]
[ApiController]
[RequirePermission(Permission.RolesRead)]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[ValidateAuthenticatedUser]
[Route("organizations/{OrganizationId:guid}/roles")]
public class OrganizationRoleController(
    IApplicationUserService applicationUserService,
    IRoleService roleService,
    IAuditService auditService,
    AuditLogResponseFactory auditLogResponseFactory,
    IValidator<OrganizationListRoleRequestDto> listRoleRequestValidator,
    IValidator<CreateRoleRequestDto> createRoleRequestValidator,
    IValidator<OrganizationRoleRequestDto> organizationRoleRequestValidator,
    IValidator<UpdateRoleRequestDto> updateRoleRequestValidator,
    IValidator<DeleteRoleRequestDto> deleteRoleRequestValidator,
    IValidator<RecoverRoleRequestDto> recoverRoleRequestValidator) : Controller
{
    [HttpGet]
    [ProducesResponseType<PagedResponseDto<RoleResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListRoles([FromQuery] OrganizationListRoleRequestDto listRoleRequestDto)
    {
        await listRoleRequestValidator.ValidateAndThrowAsync(listRoleRequestDto, HttpContext.RequestAborted);

        var roles = await roleService.ListAsync(listRoleRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(roles.ToDto(x => x.ToDto()));
    }

    [HttpGet("{RoleId:guid}")]
    [ProducesResponseType<ResponseDataDto<RoleResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRole(OrganizationRoleRequestDto organizationRoleRequestDto)
    {
        await organizationRoleRequestValidator.ValidateAndThrowAsync(organizationRoleRequestDto, HttpContext.RequestAborted);

        var role = await roleService.GetAsync(new RoleId(organizationRoleRequestDto.RoleId), new IncludeDeleted(false), HttpContext.RequestAborted);

        return Ok(role.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.RolesWrite)]
    [Idempotent]
    [HttpPost]
    [ProducesResponseType<ResponseDataDto<RoleResponseDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateRole(CreateRoleRequestDto createRoleRequestDto)
    {
        await createRoleRequestValidator.ValidateAndThrowAsync(createRoleRequestDto, HttpContext.RequestAborted);

        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        var role = await roleService.CreateAsync(
            createRoleRequestDto.ToModel(authenticatedUser.UserId), HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, role.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.RolesWrite)]
    [Idempotent]
    [HttpPatch("{RoleId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateRole(UpdateRoleRequestDto updateRoleRequestDto)
    {
        await updateRoleRequestValidator.ValidateAndThrowAsync(updateRoleRequestDto, HttpContext.RequestAborted);

        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        await roleService.UpdateAsync(updateRoleRequestDto.ToModel(authenticatedUser.UserId), HttpContext.RequestAborted);

        return NoContent();
    }

    [HttpGet("{RoleId:guid}/usages")]
    [ProducesResponseType<ResponseDataDto<RoleUsageResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoleUsages(OrganizationRoleRequestDto organizationRoleRequestDto)
    {
        await organizationRoleRequestValidator.ValidateAndThrowAsync(organizationRoleRequestDto, HttpContext.RequestAborted);

        var usage = await roleService.GetUsageAsync(new RoleId(organizationRoleRequestDto.RoleId), HttpContext.RequestAborted);

        return Ok(usage.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.RolesWrite)]
    [Idempotent]
    [HttpDelete("{RoleId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteRole(DeleteRoleRequestDto deleteRoleRequestDto)
    {
        await deleteRoleRequestValidator.ValidateAndThrowAsync(deleteRoleRequestDto, HttpContext.RequestAborted);

        await roleService.DeleteAsync(new RoleId(deleteRoleRequestDto.RoleId), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.RolesWrite)]
    [Idempotent]
    [HttpPost("{RoleId:guid}/recover")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RecoverRole(RecoverRoleRequestDto recoverRoleRequestDto)
    {
        await recoverRoleRequestValidator.ValidateAndThrowAsync(recoverRoleRequestDto, HttpContext.RequestAborted);

        await roleService.RecoverAsync(new RoleId(recoverRoleRequestDto.RoleId), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.RolesRead)]
    [RequirePermission(Permission.AuditLogsRead)]
    [HttpGet("{roleId:guid}/history")]
    [ProducesResponseType<PagedResponseDto<AuditLogResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoleHistory(
        Guid organizationId,
        Guid roleId,
        [FromQuery] ListAuditLogRequestDto listAuditLogRequestDto)
    {
        listAuditLogRequestDto.OrganizationId = organizationId;

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var auditLogs = await auditService.ListAsync(
            listAuditLogRequestDto.ToModel(AuditableEntityType.Role, roleId), HttpContext.RequestAborted);

        var resources = await auditLogResponseFactory.CreateAsync(auditLogs.Data, context, HttpContext.RequestAborted);

        return Ok(auditLogs.ToDto(resources));
    }
}
