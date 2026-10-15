using System.Net.Mime;
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
using Presentation.Mappers.Cases;
using Presentation.Mappers.Common;
using Presentation.Models.Approvals;
using Presentation.Models.AuditLogs;
using Presentation.Models.Cases;
using Presentation.Models.Common;

namespace Presentation.Controllers;

[RequirePermission(Permission.CasesRead)]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[ValidateAuthenticatedUser]
[Route("cases")]
public class CaseController(
    IApplicationUserService applicationUserService,
    ICaseService caseService,
    IAuditService auditService,
    CaseResponseFactory caseResponseFactory,
    ApprovalResponseFactory approvalResponseFactory,
    AuditLogResponseFactory auditLogResponseFactory,
    IValidator<CaseRequestDto> caseRequestValidator,
    IValidator<RecoverCaseRequestDto> recoverCaseRequestValidator,
    IValidator<ListCaseRequestDto> listCaseRequestValidator,
    IValidator<ListMyCaseRequestDto> listMyCaseRequestValidator,
    IValidator<AssignCaseRequestDto> assignCaseRequestValidator,
    IValidator<UnassignCaseRequestDto> unassignCaseRequestValidator,
    IValidator<ResolveCaseRequestDto> resolveCaseRequestValidator,
    IValidator<ChangeCaseStatusRequestDto> changeCaseStatusRequestValidator,
    IValidator<ReopenCaseRequestDto> reopenCaseRequestValidator,
    IPermissionService permissionService) : Controller
{
    [HttpGet]
    [ProducesResponseType<PagedResponseDto<CaseResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListCases([FromQuery] ListCaseRequestDto listCaseRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listCaseRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await listCaseRequestValidator.ValidateAndThrowAsync(listCaseRequestDto, HttpContext.RequestAborted);

        var permissionFilter = await permissionService.ResolveFilterAsync(
            User, Permission.CasesRead, HttpContext.RequestAborted);

        var cases = await caseService.ListAsync(
            listCaseRequestDto.ToModel() with { PermissionFilter = permissionFilter }, HttpContext.RequestAborted);

        var resources = await caseResponseFactory.CreateAsync(
            cases.Data, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(cases.ToDto(resources));
    }

    [HttpGet("my")]
    [ProducesResponseType<PagedResponseDto<CaseResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListCases([FromQuery] ListMyCaseRequestDto listMyCaseRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listMyCaseRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        listMyCaseRequestDto.AssignedToUserId = authenticatedUser.UserId;
        await listMyCaseRequestValidator.ValidateAndThrowAsync(listMyCaseRequestDto, HttpContext.RequestAborted);

        var permissionFilter = await permissionService.ResolveFilterAsync(
            User, Permission.CasesRead, HttpContext.RequestAborted);

        var cases = await caseService.ListAsync(
            listMyCaseRequestDto.ToModel() with { PermissionFilter = permissionFilter }, HttpContext.RequestAborted);

        var resources = await caseResponseFactory.CreateAsync(
            cases.Data, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(cases.ToDto(resources));
    }

    [HttpGet("{CaseId:guid}")]
    [ProducesResponseType<ResponseDataDto<CaseResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCase(CaseRequestDto caseRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        caseRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await caseRequestValidator.ValidateAndThrowAsync(caseRequestDto, HttpContext.RequestAborted);

        var @case = await caseService.GetAsync(new CaseId(caseRequestDto.CaseId), HttpContext.RequestAborted);

        var resource = await caseResponseFactory.CreateAsync(
            @case, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.CasesWrite)]
    [Idempotent]
    [HttpPost("{CaseId:guid}/assign")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AssignCase(AssignCaseRequestDto assignCaseRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        assignCaseRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        assignCaseRequestDto.AssignedByUserId = authenticatedUser.UserId;
        assignCaseRequestDto.Body.UserId ??= authenticatedUser.UserId;
        await assignCaseRequestValidator.ValidateAndThrowAsync(assignCaseRequestDto, HttpContext.RequestAborted);

        if (assignCaseRequestDto.Body.UserId != (Guid)authenticatedUser.UserId)
        {
            await permissionService.EnsureHeldOverCaseAsync(
                User, Permission.CasesAssign, new CaseId(assignCaseRequestDto.CaseId), HttpContext.RequestAborted);
        }

        await caseService.AssignAsync(new CaseId(assignCaseRequestDto.CaseId), new ApplicationUserId(assignCaseRequestDto.Body.UserId.Value), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.CasesWrite)]
    [Idempotent]
    [HttpPost("{CaseId:guid}/unassign")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UnassignCase(UnassignCaseRequestDto unassignCaseRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        unassignCaseRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await unassignCaseRequestValidator.ValidateAndThrowAsync(unassignCaseRequestDto, HttpContext.RequestAborted);

        await caseService.UnassignAsync(new CaseId(unassignCaseRequestDto.CaseId), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.CasesResolve)]
    [Idempotent]
    [HttpPost("{CaseId:guid}/resolve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ResponseDataDto<ApprovalResourceDto>>(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> ResolveCase(ResolveCaseRequestDto resolveCaseRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        resolveCaseRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        resolveCaseRequestDto.Body.UserId = authenticatedUser.UserId;
        await resolveCaseRequestValidator.ValidateAndThrowAsync(resolveCaseRequestDto, HttpContext.RequestAborted);

        var approval = await caseService.ResolveAsync(resolveCaseRequestDto.ToModel(), HttpContext.RequestAborted);

        if (approval is null)
        {
            return NoContent();
        }

        var resource = await approvalResponseFactory.CreateAsync(
            approval, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status202Accepted, resource.ToResponseData());
    }

    [RequirePermission(Permission.CasesWrite)]
    [Idempotent]
    [HttpPost("{CaseId:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangeCaseStatus(ChangeCaseStatusRequestDto changeCaseStatusRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        changeCaseStatusRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        changeCaseStatusRequestDto.Body.UserId = authenticatedUser.UserId;
        await changeCaseStatusRequestValidator.ValidateAndThrowAsync(changeCaseStatusRequestDto, HttpContext.RequestAborted);

        await caseService.ChangeStatusAsync(changeCaseStatusRequestDto.ToModel(), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.CasesWrite)]
    [Idempotent]
    [HttpPost("{CaseId:guid}/reopen")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ReopenCase(ReopenCaseRequestDto reopenCaseRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        reopenCaseRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        reopenCaseRequestDto.Body.UserId = authenticatedUser.UserId;
        await reopenCaseRequestValidator.ValidateAndThrowAsync(reopenCaseRequestDto, HttpContext.RequestAborted);

        await caseService.ReopenAsync(reopenCaseRequestDto.ToModel(), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.CasesDelete)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpDelete("{CaseId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteCase(CaseRequestDto caseRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        caseRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await caseRequestValidator.ValidateAndThrowAsync(caseRequestDto, HttpContext.RequestAborted);

        await caseService.DeleteAsync(new CaseId(caseRequestDto.CaseId), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.CasesDelete)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpPost("{CaseId:guid}/recover")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RecoverCase(RecoverCaseRequestDto recoverCaseRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        recoverCaseRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await recoverCaseRequestValidator.ValidateAndThrowAsync(recoverCaseRequestDto, HttpContext.RequestAborted);

        await caseService.RecoverAsync(new CaseId(recoverCaseRequestDto.CaseId), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.CasesEvidencePack)]
    [HttpGet("{CaseId:guid}/evidence-pack")]
    [Produces(MediaTypeNames.Application.Pdf)]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCaseEvidencePack(CaseRequestDto caseRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        caseRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await caseRequestValidator.ValidateAndThrowAsync(caseRequestDto, HttpContext.RequestAborted);

        var evidencePack = await caseService.GetEvidencePackAsync(
            new CaseId(caseRequestDto.CaseId), HttpContext.RequestAborted);

        return File(evidencePack.Content, evidencePack.ContentType, evidencePack.FileName);
    }

    [HttpGet("{caseId:guid}/history")]
    [ProducesResponseType<PagedResponseDto<AuditLogResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCaseHistory(Guid caseId, [FromQuery] ListAuditLogRequestDto listAuditLogRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listAuditLogRequestDto.OrganizationId = authenticatedUser.OrganizationId;

        var auditLogs = await auditService.ListAsync(
            listAuditLogRequestDto.ToModel(AuditableEntityType.Case, caseId), HttpContext.RequestAborted);

        var resources = await auditLogResponseFactory.CreateAsync(
            auditLogs.Data, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(auditLogs.ToDto(resources));
    }
}