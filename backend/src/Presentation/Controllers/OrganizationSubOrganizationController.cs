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
using Presentation.Mappers.SubOrganizations;
using Presentation.Models.AuditLogs;
using Presentation.Models.Common;
using Presentation.Models.SubOrganizations;

namespace Presentation.Controllers;

[SandboxMeterExempt]
[ApiController]
[RequirePermission(Permission.SubOrganizationsRead)]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[ValidateAuthenticatedUser]
[Route("organizations/{OrganizationId:guid}/sub-organizations")]
public class OrganizationSubOrganizationController(
    IApplicationUserService applicationUserService,
    ISubOrganizationService subOrganizationService,
    IAuditService auditService,
    AuditLogResponseFactory auditLogResponseFactory,
    IValidator<OrganizationListSubOrganizationRequestDto> listSubOrganizationRequestValidator,
    IValidator<CreateSubOrganizationRequestDto> createSubOrganizationRequestValidator,
    IValidator<SubOrganizationRequestDto> subOrganizationRequestValidator,
    IValidator<UpdateSubOrganizationRequestDto> updateSubOrganizationRequestValidator,
    IValidator<DeleteSubOrganizationRequestDto> deleteSubOrganizationRequestValidator,
    IValidator<RecoverSubOrganizationRequestDto> recoverSubOrganizationRequestValidator) : Controller
{
    [HttpGet]
    [ProducesResponseType<PagedResponseDto<SubOrganizationResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListSubOrganizations([FromQuery] OrganizationListSubOrganizationRequestDto listSubOrganizationRequestDto)
    {
        await listSubOrganizationRequestValidator.ValidateAndThrowAsync(listSubOrganizationRequestDto, HttpContext.RequestAborted);

        var subOrganizations = await subOrganizationService.ListAsync(listSubOrganizationRequestDto.ToModel(), HttpContext.RequestAborted);

        return Ok(subOrganizations.ToDto(x => x.ToDto()));
    }

    [HttpGet("{SubOrganizationId:guid}")]
    [ProducesResponseType<ResponseDataDto<SubOrganizationResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubOrganization(SubOrganizationRequestDto subOrganizationRequestDto)
    {
        await subOrganizationRequestValidator.ValidateAndThrowAsync(subOrganizationRequestDto, HttpContext.RequestAborted);

        var subOrganization = await subOrganizationService.GetAsync(
            new SubOrganizationId(subOrganizationRequestDto.SubOrganizationId), new IncludeDeleted(false), HttpContext.RequestAborted);

        return Ok(subOrganization.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.SubOrganizationsWrite)]
    [Idempotent]
    [HttpPost]
    [ProducesResponseType<ResponseDataDto<SubOrganizationResponseDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSubOrganization(CreateSubOrganizationRequestDto createSubOrganizationRequestDto)
    {
        await createSubOrganizationRequestValidator.ValidateAndThrowAsync(createSubOrganizationRequestDto, HttpContext.RequestAborted);

        var subOrganization = await subOrganizationService.CreateAsync(
            createSubOrganizationRequestDto.ToModel(), HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, subOrganization.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.SubOrganizationsWrite)]
    [Idempotent]
    [HttpPatch("{SubOrganizationId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateSubOrganization(UpdateSubOrganizationRequestDto updateSubOrganizationRequestDto)
    {
        await updateSubOrganizationRequestValidator.ValidateAndThrowAsync(updateSubOrganizationRequestDto, HttpContext.RequestAborted);

        await subOrganizationService.UpdateAsync(updateSubOrganizationRequestDto.ToModel(), HttpContext.RequestAborted);

        return NoContent();
    }

    [HttpGet("{SubOrganizationId:guid}/usages")]
    [ProducesResponseType<ResponseDataDto<SubOrganizationUsageResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubOrganizationUsages(SubOrganizationRequestDto subOrganizationRequestDto)
    {
        await subOrganizationRequestValidator.ValidateAndThrowAsync(subOrganizationRequestDto, HttpContext.RequestAborted);

        var usage = await subOrganizationService.GetUsageAsync(
            new SubOrganizationId(subOrganizationRequestDto.SubOrganizationId), HttpContext.RequestAborted);

        return Ok(usage.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.SubOrganizationsWrite)]
    [Idempotent]
    [HttpDelete("{SubOrganizationId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteSubOrganization(DeleteSubOrganizationRequestDto deleteSubOrganizationRequestDto)
    {
        await deleteSubOrganizationRequestValidator.ValidateAndThrowAsync(deleteSubOrganizationRequestDto, HttpContext.RequestAborted);

        await subOrganizationService.DeleteAsync(
            new SubOrganizationId(deleteSubOrganizationRequestDto.SubOrganizationId), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.SubOrganizationsWrite)]
    [Idempotent]
    [HttpPost("{SubOrganizationId:guid}/recover")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RecoverSubOrganization(RecoverSubOrganizationRequestDto recoverSubOrganizationRequestDto)
    {
        await recoverSubOrganizationRequestValidator.ValidateAndThrowAsync(recoverSubOrganizationRequestDto, HttpContext.RequestAborted);

        await subOrganizationService.RecoverAsync(
            new SubOrganizationId(recoverSubOrganizationRequestDto.SubOrganizationId), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.SubOrganizationsRead)]
    [RequirePermission(Permission.AuditLogsRead)]
    [HttpGet("{subOrganizationId:guid}/history")]
    [ProducesResponseType<PagedResponseDto<AuditLogResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubOrganizationHistory(
        Guid organizationId,
        Guid subOrganizationId,
        [FromQuery] ListAuditLogRequestDto listAuditLogRequestDto)
    {
        listAuditLogRequestDto.OrganizationId = organizationId;

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var auditLogs = await auditService.ListAsync(
            listAuditLogRequestDto.ToModel(AuditableEntityType.SubOrganization, subOrganizationId), HttpContext.RequestAborted);

        var resources = await auditLogResponseFactory.CreateAsync(auditLogs.Data, context, HttpContext.RequestAborted);

        return Ok(auditLogs.ToDto(resources));
    }
}
