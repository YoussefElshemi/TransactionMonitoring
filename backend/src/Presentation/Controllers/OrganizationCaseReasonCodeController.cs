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
using Presentation.Mappers.CaseReasonCodes;
using Presentation.Mappers.Common;
using Presentation.Models.AuditLogs;
using Presentation.Models.CaseReasonCodes;
using Presentation.Models.Common;

namespace Presentation.Controllers;

[RequirePermission(Permission.CaseReasonCodesRead)]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[Route("organizations/{OrganizationId:guid}/reason-codes")]
public class OrganizationCaseReasonCodeController(
    IApplicationUserService applicationUserService,
    ICaseReasonCodeService caseReasonCodeService,
    IAuditService auditService,
    CaseReasonCodeResponseFactory caseReasonCodeResponseFactory,
    AuditLogResponseFactory auditLogResponseFactory,
    IValidator<ListCaseReasonCodeRequestDto> listCaseReasonCodeRequestValidator,
    IValidator<CreateCaseReasonCodeRequestDto> createCaseReasonCodeRequestValidator,
    IValidator<OrganizationCaseReasonCodeRequestDto> organizationCaseReasonCodeRequestValidator,
    IValidator<UpdateCaseReasonCodeRequestDto> updateCaseReasonCodeRequestValidator,
    IValidator<DeleteCaseReasonCodeRequestDto> deleteCaseReasonCodeRequestValidator,
    IValidator<RecoverCaseReasonCodeRequestDto> recoverCaseReasonCodeRequestValidator) : Controller
{
    [HttpGet]
    [ProducesResponseType<PagedResponseDto<CaseReasonCodeResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListCaseReasonCodes([FromQuery] ListCaseReasonCodeRequestDto listCaseReasonCodeRequestDto)
    {
        await listCaseReasonCodeRequestValidator.ValidateAndThrowAsync(listCaseReasonCodeRequestDto, HttpContext.RequestAborted);

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var caseReasonCodes = await caseReasonCodeService.ListAsync(listCaseReasonCodeRequestDto.ToModel(), HttpContext.RequestAborted);

        var resources = await caseReasonCodeResponseFactory.CreateAsync(
            caseReasonCodes.Data, context, HttpContext.RequestAborted);

        return Ok(caseReasonCodes.ToDto(resources));
    }

    [HttpGet("{CaseReasonCodeId:guid}")]
    [ProducesResponseType<ResponseDataDto<CaseReasonCodeResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCaseReasonCode(OrganizationCaseReasonCodeRequestDto organizationCaseReasonCodeRequestDto)
    {
        await organizationCaseReasonCodeRequestValidator.ValidateAndThrowAsync(organizationCaseReasonCodeRequestDto, HttpContext.RequestAborted);

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var caseReasonCode = await caseReasonCodeService.GetAsync(
            new CaseReasonCodeId(organizationCaseReasonCodeRequestDto.CaseReasonCodeId), new IncludeDeleted(false), HttpContext.RequestAborted);

        var resource = await caseReasonCodeResponseFactory.CreateAsync(caseReasonCode, context, HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.CaseReasonCodesWrite)]
    [Idempotent]
    [HttpPost]
    [ProducesResponseType<ResponseDataDto<CaseReasonCodeResponseDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCaseReasonCode(CreateCaseReasonCodeRequestDto createCaseReasonCodeRequestDto)
    {
        await createCaseReasonCodeRequestValidator.ValidateAndThrowAsync(createCaseReasonCodeRequestDto, HttpContext.RequestAborted);

        var caseReasonCode = await caseReasonCodeService.CreateAsync(createCaseReasonCodeRequestDto.ToModel(), HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, caseReasonCode.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.CaseReasonCodesWrite)]
    [Idempotent]
    [HttpPatch("{CaseReasonCodeId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateCaseReasonCode(UpdateCaseReasonCodeRequestDto updateCaseReasonCodeRequestDto)
    {
        await updateCaseReasonCodeRequestValidator.ValidateAndThrowAsync(updateCaseReasonCodeRequestDto, HttpContext.RequestAborted);

        await caseReasonCodeService.UpdateAsync(updateCaseReasonCodeRequestDto.ToModel(), HttpContext.RequestAborted);

        return NoContent();
    }

    [HttpGet("{CaseReasonCodeId:guid}/usages")]
    [ProducesResponseType<ResponseDataDto<CaseReasonCodeUsageResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCaseReasonCodeUsages(OrganizationCaseReasonCodeRequestDto organizationCaseReasonCodeRequestDto)
    {
        await organizationCaseReasonCodeRequestValidator.ValidateAndThrowAsync(organizationCaseReasonCodeRequestDto, HttpContext.RequestAborted);

        var usage = await caseReasonCodeService.GetUsageAsync(
            new CaseReasonCodeId(organizationCaseReasonCodeRequestDto.CaseReasonCodeId), HttpContext.RequestAborted);

        return Ok(usage.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.CaseReasonCodesWrite)]
    [Idempotent]
    [HttpDelete("{CaseReasonCodeId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteCaseReasonCode(DeleteCaseReasonCodeRequestDto deleteCaseReasonCodeRequestDto)
    {
        await deleteCaseReasonCodeRequestValidator.ValidateAndThrowAsync(deleteCaseReasonCodeRequestDto, HttpContext.RequestAborted);

        await caseReasonCodeService.DeleteAsync(
            new CaseReasonCodeId(deleteCaseReasonCodeRequestDto.CaseReasonCodeId), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.CaseReasonCodesWrite)]
    [Idempotent]
    [HttpPost("{CaseReasonCodeId:guid}/recover")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RecoverCaseReasonCode(RecoverCaseReasonCodeRequestDto recoverCaseReasonCodeRequestDto)
    {
        await recoverCaseReasonCodeRequestValidator.ValidateAndThrowAsync(recoverCaseReasonCodeRequestDto, HttpContext.RequestAborted);

        await caseReasonCodeService.RecoverAsync(
            new CaseReasonCodeId(recoverCaseReasonCodeRequestDto.CaseReasonCodeId), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.CaseReasonCodesRead)]
    [RequirePermission(Permission.AuditLogsRead)]
    [HttpGet("{caseReasonCodeId:guid}/history")]
    [ProducesResponseType<PagedResponseDto<AuditLogResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCaseReasonCodeHistory(
        Guid organizationId,
        Guid caseReasonCodeId,
        [FromQuery] ListAuditLogRequestDto listAuditLogRequestDto)
    {
        listAuditLogRequestDto.OrganizationId = organizationId;

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var auditLogs = await auditService.ListAsync(
            listAuditLogRequestDto.ToModel(AuditableEntityType.CaseReasonCode, caseReasonCodeId), HttpContext.RequestAborted);

        var resources = await auditLogResponseFactory.CreateAsync(auditLogs.Data, context, HttpContext.RequestAborted);

        return Ok(auditLogs.ToDto(resources));
    }
}
