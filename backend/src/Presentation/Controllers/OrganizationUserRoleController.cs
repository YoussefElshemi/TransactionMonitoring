using Core.Enums;
using Core.Interfaces.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Mappers.Common;
using Presentation.Mappers.Roles;
using Presentation.Models.Common;
using Presentation.Models.Roles;

namespace Presentation.Controllers;

[SandboxMeterExempt]
[ApiController]
[RequirePermission(Permission.RolesRead)]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[ValidateAuthenticatedUser]
[Route("organizations/{OrganizationId:guid}")]
public class OrganizationUserRoleController(
    IApplicationUserService applicationUserService,
    IRoleAssignmentService roleAssignmentService,
    IValidator<OrganizationListRoleAssignmentRequestDto> listRoleAssignmentRequestValidator,
    IValidator<AssignRoleRequestDto> assignRoleRequestValidator,
    IValidator<RevokeRoleRequestDto> revokeRoleRequestValidator) : Controller
{
    [HttpGet("role-assignments")]
    [ProducesResponseType<PagedResponseDto<RoleAssignmentResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListRoleAssignments([FromQuery] OrganizationListRoleAssignmentRequestDto listRoleAssignmentRequestDto)
    {
        await listRoleAssignmentRequestValidator.ValidateAndThrowAsync(listRoleAssignmentRequestDto, HttpContext.RequestAborted);

        var assignments = await roleAssignmentService.ListAsync(listRoleAssignmentRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(assignments.ToDto(x => x.ToDto()));
    }

    [HttpGet("users/{UserId:guid}/role-assignments")]
    [ProducesResponseType<PagedResponseDto<RoleAssignmentResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListUserRoleAssignments(
        Guid userId,
        [FromQuery] OrganizationListRoleAssignmentRequestDto listRoleAssignmentRequestDto)
    {
        listRoleAssignmentRequestDto.UserId = userId;
        await listRoleAssignmentRequestValidator.ValidateAndThrowAsync(listRoleAssignmentRequestDto, HttpContext.RequestAborted);

        var assignments = await roleAssignmentService.ListAsync(listRoleAssignmentRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(assignments.ToDto(x => x.ToDto()));
    }

    [RequirePermission(Permission.RolesAssign)]
    [Idempotent]
    [HttpPost("users/{UserId:guid}/role-assignments")]
    [ProducesResponseType<ResponseDataDto<RoleAssignmentResponseDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> AssignRole(AssignRoleRequestDto assignRoleRequestDto)
    {
        await assignRoleRequestValidator.ValidateAndThrowAsync(assignRoleRequestDto, HttpContext.RequestAborted);

        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        var assignment = await roleAssignmentService.AssignAsync(
            assignRoleRequestDto.ToModel(authenticatedUser.UserId), HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, assignment.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.RolesAssign)]
    [Idempotent]
    [HttpDelete("role-assignments/{RoleAssignmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeRole(RevokeRoleRequestDto revokeRoleRequestDto)
    {
        await revokeRoleRequestValidator.ValidateAndThrowAsync(revokeRoleRequestDto, HttpContext.RequestAborted);

        await roleAssignmentService.RevokeAsync(revokeRoleRequestDto.ToModel(), HttpContext.RequestAborted);

        return NoContent();
    }
}
