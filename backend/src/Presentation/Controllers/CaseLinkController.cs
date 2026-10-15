using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.CaseLinks;
using Core.ValueObjects;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Extensions;
using Presentation.Factories;
using Presentation.Mappers.AuditLogs;
using Presentation.Mappers.CaseLinks;
using Presentation.Mappers.Common;
using Presentation.Models.AuditLogs;
using Presentation.Models.CaseLinks;
using Presentation.Models.Common;

namespace Presentation.Controllers;

[RequirePermission(Permission.CaseLinksRead)]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[ValidateAuthenticatedUser]
[Route("cases/{CaseId:guid}/links")]
public class CaseLinkController(
    IApplicationUserService applicationUserService,
    ICaseLinkService caseLinkService,
    IAuditService auditService,
    CaseLinkResponseFactory caseLinkResponseFactory,
    AuditLogResponseFactory auditLogResponseFactory,
    IValidator<CreateCaseLinkRequestDto> createCaseLinkRequestValidator,
    IValidator<ListCaseLinkRequestDto> listCaseLinksRequestValidator,
    IValidator<CaseLinkRequestDto> caseLinkRequestValidator,
    IValidator<DeleteCaseLinkRequestDto> deleteCaseLinkRequestValidator) : Controller
{
    [RequirePermission(Permission.CaseLinksWrite)]
    [Idempotent]
    [HttpPost]
    [ProducesResponseType<ResponseDataDto<CaseLinkResourceDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCaseLink(CreateCaseLinkRequestDto createCaseLinkRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        createCaseLinkRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await createCaseLinkRequestValidator.ValidateAndThrowAsync(createCaseLinkRequestDto, HttpContext.RequestAborted);

        var createCaseLinkRequest = new CreateCaseLinkRequest
        {
            OrganizationId = authenticatedUser.OrganizationId,
            CaseId = new CaseId(createCaseLinkRequestDto.CaseId),
            LinkedCaseId = new CaseId(createCaseLinkRequestDto.Body.LinkedCaseId),
            Reason = string.IsNullOrWhiteSpace(createCaseLinkRequestDto.Body.Reason)
                ? null
                : new LinkReason(createCaseLinkRequestDto.Body.Reason),
            CreatedByUserId = authenticatedUser.UserId
        };

        var caseLink = await caseLinkService.CreateAsync(createCaseLinkRequest, HttpContext.RequestAborted);

        var resource = await caseLinkResponseFactory.CreateAsync(
            caseLink, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, resource.ToResponseData());
    }

    [HttpGet]
    [ProducesResponseType<PagedResponseDto<CaseLinkResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListCaseLinks([FromRoute] ListCaseLinkRequestDto listCaseLinkRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listCaseLinkRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await listCaseLinksRequestValidator.ValidateAndThrowAsync(listCaseLinkRequestDto, HttpContext.RequestAborted);

        var caseLinks = await caseLinkService.ListAsync(listCaseLinkRequestDto.ToModel(), HttpContext.RequestAborted);

        var resources = await caseLinkResponseFactory.CreateAsync(
            caseLinks.Data, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(caseLinks.ToDto(resources));
    }

    [HttpGet("{CaseLinkId:guid}")]
    [ProducesResponseType<ResponseDataDto<CaseLinkResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCaseLink(CaseLinkRequestDto caseLinkRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        caseLinkRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await caseLinkRequestValidator.ValidateAndThrowAsync(caseLinkRequestDto, HttpContext.RequestAborted);

        var caseLink = await caseLinkService.GetAsync(
            new CaseLinkId(caseLinkRequestDto.CaseLinkId), new CaseId(caseLinkRequestDto.CaseId), HttpContext.RequestAborted);

        var resource = await caseLinkResponseFactory.CreateAsync(
            caseLink, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.CaseLinksWrite)]
    [Idempotent]
    [HttpDelete("{CaseLinkId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteCaseLink(DeleteCaseLinkRequestDto deleteCaseLinkRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        deleteCaseLinkRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await deleteCaseLinkRequestValidator.ValidateAndThrowAsync(deleteCaseLinkRequestDto, HttpContext.RequestAborted);

        await caseLinkService.DeleteAsync(new CaseLinkId(deleteCaseLinkRequestDto.CaseLinkId), HttpContext.RequestAborted);

        return NoContent();
    }

    [HttpGet("{caseLinkId:guid}/history")]
    [ProducesResponseType<PagedResponseDto<AuditLogResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCaseLinkHistory(Guid caseLinkId, [FromQuery] ListAuditLogRequestDto listAuditLogRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listAuditLogRequestDto.OrganizationId = authenticatedUser.OrganizationId;

        var auditLogs = await auditService.ListAsync(
            listAuditLogRequestDto.ToModel(AuditableEntityType.CaseLink, caseLinkId), HttpContext.RequestAborted);

        var resources = await auditLogResponseFactory.CreateAsync(
            auditLogs.Data, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(auditLogs.ToDto(resources));
    }
}
