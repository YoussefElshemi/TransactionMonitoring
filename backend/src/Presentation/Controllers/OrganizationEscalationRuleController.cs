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
using Presentation.Mappers.Escalations;
using Presentation.Models.AuditLogs;
using Presentation.Models.Common;
using Presentation.Models.Escalations;

namespace Presentation.Controllers;

[RequirePermission(Permission.EscalationRulesRead)]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[Route("organizations/{OrganizationId:guid}/escalation-rules")]
public class OrganizationEscalationRuleController(
    IApplicationUserService applicationUserService,
    IEscalationRuleService escalationRuleService,
    IAuditService auditService,
    EscalationRuleResponseFactory escalationRuleResponseFactory,
    AuditLogResponseFactory auditLogResponseFactory,
    IValidator<ListEscalationRuleRequestDto> listEscalationRuleRequestValidator,
    IValidator<CreateEscalationRuleRequestDto> createEscalationRuleRequestValidator,
    IValidator<OrganizationEscalationRuleRequestDto> organizationEscalationRuleRequestValidator,
    IValidator<UpdateEscalationRuleRequestDto> updateEscalationRuleRequestValidator,
    IValidator<DeleteEscalationRuleRequestDto> deleteEscalationRuleRequestValidator,
    IValidator<RecoverEscalationRuleRequestDto> recoverEscalationRuleRequestValidator) : Controller
{
    [HttpGet]
    [ProducesResponseType<PagedResponseDto<EscalationRuleResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListEscalationRules([FromQuery] ListEscalationRuleRequestDto listEscalationRuleRequestDto)
    {
        await listEscalationRuleRequestValidator.ValidateAndThrowAsync(listEscalationRuleRequestDto, HttpContext.RequestAborted);

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var escalationRules = await escalationRuleService.ListAsync(listEscalationRuleRequestDto.ToModel(), HttpContext.RequestAborted);

        var resources = await escalationRuleResponseFactory.CreateAsync(escalationRules.Data, context, HttpContext.RequestAborted);

        return Ok(escalationRules.ToDto(resources));
    }

    [HttpGet("{EscalationRuleId:guid}")]
    [ProducesResponseType<ResponseDataDto<EscalationRuleResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEscalationRule(OrganizationEscalationRuleRequestDto organizationEscalationRuleRequestDto)
    {
        await organizationEscalationRuleRequestValidator.ValidateAndThrowAsync(organizationEscalationRuleRequestDto, HttpContext.RequestAborted);

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var escalationRule = await escalationRuleService.GetAsync(
            new EscalationRuleId(organizationEscalationRuleRequestDto.EscalationRuleId), new IncludeDeleted(false), HttpContext.RequestAborted);

        var resource = await escalationRuleResponseFactory.CreateAsync(escalationRule, context, HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.EscalationRulesWrite)]
    [Idempotent]
    [HttpPost]
    [ProducesResponseType<ResponseDataDto<EscalationRuleResponseDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateEscalationRule(CreateEscalationRuleRequestDto createEscalationRuleRequestDto)
    {
        await createEscalationRuleRequestValidator.ValidateAndThrowAsync(createEscalationRuleRequestDto, HttpContext.RequestAborted);

        var escalationRule = await escalationRuleService.CreateAsync(createEscalationRuleRequestDto.ToModel(), HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, escalationRule.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.EscalationRulesWrite)]
    [Idempotent]
    [HttpPatch("{EscalationRuleId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateEscalationRule(UpdateEscalationRuleRequestDto updateEscalationRuleRequestDto)
    {
        await updateEscalationRuleRequestValidator.ValidateAndThrowAsync(updateEscalationRuleRequestDto, HttpContext.RequestAborted);

        await escalationRuleService.UpdateAsync(updateEscalationRuleRequestDto.ToModel(), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.EscalationRulesWrite)]
    [Idempotent]
    [HttpDelete("{EscalationRuleId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteEscalationRule(DeleteEscalationRuleRequestDto deleteEscalationRuleRequestDto)
    {
        await deleteEscalationRuleRequestValidator.ValidateAndThrowAsync(deleteEscalationRuleRequestDto, HttpContext.RequestAborted);

        await escalationRuleService.DeleteAsync(new EscalationRuleId(deleteEscalationRuleRequestDto.EscalationRuleId), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.EscalationRulesWrite)]
    [Idempotent]
    [HttpPost("{EscalationRuleId:guid}/recover")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RecoverEscalationRule(RecoverEscalationRuleRequestDto recoverEscalationRuleRequestDto)
    {
        await recoverEscalationRuleRequestValidator.ValidateAndThrowAsync(recoverEscalationRuleRequestDto, HttpContext.RequestAborted);

        await escalationRuleService.RecoverAsync(new EscalationRuleId(recoverEscalationRuleRequestDto.EscalationRuleId), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.EscalationRulesRead)]
    [RequirePermission(Permission.AuditLogsRead)]
    [HttpGet("{escalationRuleId:guid}/history")]
    [ProducesResponseType<PagedResponseDto<AuditLogResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEscalationRuleHistory(
        Guid organizationId,
        Guid escalationRuleId,
        [FromQuery] ListAuditLogRequestDto listAuditLogRequestDto)
    {
        listAuditLogRequestDto.OrganizationId = organizationId;

        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var auditLogs = await auditService.ListAsync(
            listAuditLogRequestDto.ToModel(AuditableEntityType.EscalationRule, escalationRuleId), HttpContext.RequestAborted);

        var resources = await auditLogResponseFactory.CreateAsync(auditLogs.Data, context, HttpContext.RequestAborted);

        return Ok(auditLogs.ToDto(resources));
    }
}
