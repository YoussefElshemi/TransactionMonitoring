using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.CaseNotes;
using Core.ValueObjects;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Extensions;
using Presentation.Factories;
using Presentation.Mappers.AuditLogs;
using Presentation.Mappers.CaseNotes;
using Presentation.Mappers.Common;
using Presentation.Models.AuditLogs;
using Presentation.Models.CaseNotes;
using Presentation.Models.Common;

namespace Presentation.Controllers;

[RequirePermission(Permission.CaseNotesRead)]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[ValidateAuthenticatedUser]
[Route("cases/{CaseId:guid}/notes")]
public class CaseNoteController(
    IApplicationUserService applicationUserService,
    ICaseNoteService caseNoteService,
    IAuditService auditService,
    CaseNoteResponseFactory caseNoteResponseFactory,
    AuditLogResponseFactory auditLogResponseFactory,
    IValidator<CreateCaseNoteRequestDto> createCaseNoteRequestValidator,
    IValidator<ListCaseNoteRequestDto> listCaseNotesRequestValidator,
    IValidator<CaseNoteRequestDto> caseNoteRequestValidator,
    IValidator<RecoverCaseNoteRequestDto> recoverCaseNoteRequestValidator,
    IValidator<UpdateCaseNoteRequestDto> updateCaseNoteRequestValidator) : Controller
{
    [RequirePermission(Permission.CaseNotesWrite)]
    [Idempotent]
    [HttpPost]
    [ProducesResponseType<ResponseDataDto<CaseNoteResourceDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCaseNote(CreateCaseNoteRequestDto createCaseNoteRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        createCaseNoteRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await createCaseNoteRequestValidator.ValidateAndThrowAsync(createCaseNoteRequestDto, HttpContext.RequestAborted);

        var createCaseNoteRequest = new CreateCaseNoteRequest
        {
            CaseId = new CaseId(createCaseNoteRequestDto.CaseId),
            Message = new Message(createCaseNoteRequestDto.Body.Message),
            OrganizationId = authenticatedUser.OrganizationId,
            CreatedByUserId = authenticatedUser.UserId
        };

        var caseNote = await caseNoteService.CreateAsync(createCaseNoteRequest, HttpContext.RequestAborted);

        var resource = await caseNoteResponseFactory.CreateAsync(
            caseNote, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, resource.ToResponseData());
    }

    [HttpGet]
    [ProducesResponseType<PagedResponseDto<CaseNoteResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListCaseNotes([FromRoute] ListCaseNoteRequestDto listCaseNoteRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listCaseNoteRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await listCaseNotesRequestValidator.ValidateAndThrowAsync(listCaseNoteRequestDto, HttpContext.RequestAborted);

        var notes = await caseNoteService.ListAsync(listCaseNoteRequestDto.ToModel(), HttpContext.RequestAborted);

        var resources = await caseNoteResponseFactory.CreateAsync(
            notes.Data, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(notes.ToDto(resources));
    }

    [HttpGet("{CaseNoteId:guid}")]
    [ProducesResponseType<ResponseDataDto<CaseNoteResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCaseNote(CaseNoteRequestDto caseNoteRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        caseNoteRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await caseNoteRequestValidator.ValidateAndThrowAsync(caseNoteRequestDto, HttpContext.RequestAborted);

        var caseNote = await caseNoteService.GetAsync(new CaseNoteId(caseNoteRequestDto.CaseNoteId), HttpContext.RequestAborted);

        var resource = await caseNoteResponseFactory.CreateAsync(
            caseNote, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.CaseNotesWrite)]
    [Idempotent]
    [HttpPatch("{CaseNoteId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateCaseNote(UpdateCaseNoteRequestDto updateCaseNoteRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        updateCaseNoteRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await updateCaseNoteRequestValidator.ValidateAndThrowAsync(updateCaseNoteRequestDto, HttpContext.RequestAborted);

        await caseNoteService.UpdateAsync(updateCaseNoteRequestDto.ToModel(), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.CaseNotesWrite)]
    [Idempotent]
    [HttpDelete("{CaseNoteId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteCaseNote(CaseNoteRequestDto caseNoteRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        caseNoteRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await caseNoteRequestValidator.ValidateAndThrowAsync(caseNoteRequestDto, HttpContext.RequestAborted);

        await caseNoteService.DeleteAsync(new CaseNoteId(caseNoteRequestDto.CaseNoteId), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.CaseNotesWrite)]
    [Idempotent]
    [HttpPost("{CaseNoteId:guid}/recover")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RecoverCaseNote(RecoverCaseNoteRequestDto recoverCaseNoteRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        recoverCaseNoteRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await recoverCaseNoteRequestValidator.ValidateAndThrowAsync(recoverCaseNoteRequestDto, HttpContext.RequestAborted);

        await caseNoteService.RecoverAsync(new CaseNoteId(recoverCaseNoteRequestDto.CaseNoteId), HttpContext.RequestAborted);

        return NoContent();
    }

    [HttpGet("{caseNoteId:guid}/history")]
    [ProducesResponseType<PagedResponseDto<AuditLogResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCaseNoteHistory(Guid caseNoteId, [FromQuery] ListAuditLogRequestDto listAuditLogRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listAuditLogRequestDto.OrganizationId = authenticatedUser.OrganizationId;

        var auditLogs = await auditService.ListAsync(
            listAuditLogRequestDto.ToModel(AuditableEntityType.CaseNote, caseNoteId), HttpContext.RequestAborted);

        var resources = await auditLogResponseFactory.CreateAsync(
            auditLogs.Data, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(auditLogs.ToDto(resources));
    }
}