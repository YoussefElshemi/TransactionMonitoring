using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.RuleSets;
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
using Presentation.Mappers.RuleSets;
using Presentation.Models.AuditLogs;
using Presentation.Models.Common;
using Presentation.Models.Rules;
using Presentation.Models.RuleSets;

namespace Presentation.Controllers;

[ApiController]
[Authorize]
[ValidateAuthenticatedUser]
[Route("rule-sets")]
public class RuleSetController(
    IApplicationUserService applicationUserService,
    IRuleSetService ruleSetService,
    IAuditService auditService,
    RuleSetResponseFactory ruleSetResponseFactory,
    AuditLogResponseFactory auditLogResponseFactory,
    EvaluateResponseFactory evaluateResponseFactory,
    IValidator<CreateRuleSetRequestDto> createRuleSetRequestValidator,
    IValidator<RuleSetRequestDto> ruleSetRequestValidator,
    IValidator<RecoverRuleSetRequestDto> recoverRuleSetRequestValidator,
    IValidator<UpdateRuleSetRequestDto> updateRuleSetRequestValidator,
    IValidator<EvaluateRuleSetRequestDto> evaluateRuleSetRequestValidator,
    IValidator<ListRuleSetRequestDto> listRuleSetsRequestValidator,
    IPermissionService permissionService) : Controller
{
    [RequirePermission(Permission.RuleSetsEvaluate)]
    [Authorize(Policy = AuthConstants.ApiScopePolicies.RuleSetsEvaluate)]
    [RateLimit(RateLimitPool.Evaluation)]
    [Idempotent]
    [SandboxMeterExempt]
    [HttpPost("evaluate")]
    [ProducesResponseType<ResponseDataDto<EvaluateAllRulesResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> EvaluateAllRuleSets(EvaluateAllRuleSetRequestDto evaluateAllRuleSetRequestDto)
    {
        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        var evaluateRuleSetsRequest = new EvaluateRuleSetsRequest
        {
            OrganizationId = context.OrganizationId,
            Payload = evaluateAllRuleSetRequestDto.Payload,
            ExternalReference = evaluateAllRuleSetRequestDto.TransactionReference != null ? new ExternalReference(evaluateAllRuleSetRequestDto.TransactionReference) : null,
            CreateCase = new CreateCase(evaluateAllRuleSetRequestDto.CreateCase)
        };

        var response = await ruleSetService.EvaluateAllAsync(evaluateRuleSetsRequest, HttpContext.RequestAborted);

        var resource = await evaluateResponseFactory.CreateAsync(response, context, HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.RuleSetsWrite)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpPost]
    [ProducesResponseType<ResponseDataDto<RuleSetResourceDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateRuleSet([FromBody] CreateRuleSetRequestDto createRuleSetRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        createRuleSetRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await createRuleSetRequestValidator.ValidateAndThrowAsync(createRuleSetRequestDto, HttpContext.RequestAborted);

        var createRuleSetRequest = new CreateRuleSetRequest
        {
            FriendlyName = new FriendlyName(createRuleSetRequestDto.FriendlyName),
            OrganizationId = authenticatedUser.OrganizationId,
            IsEnabled = new IsEnabled(createRuleSetRequestDto.IsEnabled),
            ReviewThreshold = createRuleSetRequestDto.ReviewThreshold.HasValue ? new ReviewThreshold(createRuleSetRequestDto.ReviewThreshold.Value) : null,
            CreatedByUserId = authenticatedUser.UserId,
            SubOrganizationId = createRuleSetRequestDto.SubOrganizationId.HasValue ? new SubOrganizationId(createRuleSetRequestDto.SubOrganizationId.Value) : null
        };

        var ruleSet = await ruleSetService.CreateAsync(createRuleSetRequest, HttpContext.RequestAborted);

        var resource = await ruleSetResponseFactory.CreateAsync(
            ruleSet, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, resource.ToResponseData());
    }

    [RequirePermission(Permission.RuleSetsRead)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [HttpGet]
    [ProducesResponseType<PagedResponseDto<RuleSetResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListRuleSets([FromQuery] ListRuleSetRequestDto listRuleSetRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listRuleSetRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await listRuleSetsRequestValidator.ValidateAndThrowAsync(listRuleSetRequestDto, HttpContext.RequestAborted);

        var permissionFilter = await permissionService.ResolveFilterAsync(
            User, Permission.RuleSetsRead, HttpContext.RequestAborted);

        var ruleSets = await ruleSetService.ListAsync(
            listRuleSetRequestDto.ToModel() with { PermissionFilter = permissionFilter }, HttpContext.RequestAborted);

        var resources = await ruleSetResponseFactory.CreateAsync(
            ruleSets.Data, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(ruleSets.ToDto(resources));
    }

    [RequirePermission(Permission.RuleSetsRead)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [HttpGet("{RuleSetId:guid}")]
    [ProducesResponseType<ResponseDataDto<RuleSetResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRuleSet(RuleSetRequestDto ruleSetRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        ruleSetRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await ruleSetRequestValidator.ValidateAndThrowAsync(ruleSetRequestDto, HttpContext.RequestAborted);

        var ruleSet = await ruleSetService.GetAsync(new RuleSetId(ruleSetRequestDto.RuleSetId), HttpContext.RequestAborted);

        var resource = await ruleSetResponseFactory.CreateAsync(
            ruleSet, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.RuleSetsWrite)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpPatch("{RuleSetId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateRuleSet(UpdateRuleSetRequestDto updateRuleSetRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        updateRuleSetRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await updateRuleSetRequestValidator.ValidateAndThrowAsync(updateRuleSetRequestDto, HttpContext.RequestAborted);

        await ruleSetService.UpdateAsync(updateRuleSetRequestDto.ToModel(), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.RuleSetsWrite)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpDelete("{RuleSetId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteRuleSet(RuleSetRequestDto ruleSetRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        ruleSetRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await ruleSetRequestValidator.ValidateAndThrowAsync(ruleSetRequestDto, HttpContext.RequestAborted);

        await ruleSetService.DeleteAsync(new RuleSetId(ruleSetRequestDto.RuleSetId), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.RuleSetsWrite)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpPost("{RuleSetId:guid}/recover")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RecoverRuleSet(RecoverRuleSetRequestDto recoverRuleSetRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        recoverRuleSetRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await recoverRuleSetRequestValidator.ValidateAndThrowAsync(recoverRuleSetRequestDto, HttpContext.RequestAborted);

        await ruleSetService.RecoverAsync(new RuleSetId(recoverRuleSetRequestDto.RuleSetId), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.RuleSetsEvaluate)]
    [Authorize(Policy = AuthConstants.ApiScopePolicies.RuleSetsEvaluate)]
    [RateLimit(RateLimitPool.Evaluation)]
    [Idempotent]
    [SandboxMeterExempt]
    [HttpPost("{RuleSetId:guid}/evaluate")]
    [ProducesResponseType<ResponseDataDto<EvaluateRuleSetResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> EvaluateRuleSet(EvaluateRuleSetRequestDto evaluateRuleSetRequestDto)
    {
        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        evaluateRuleSetRequestDto.OrganizationId = context.OrganizationId;
        await evaluateRuleSetRequestValidator.ValidateAndThrowAsync(evaluateRuleSetRequestDto, HttpContext.RequestAborted);

        var evaluateRuleSetRequest = new EvaluateRuleSetRequest
        {
            RuleSetId = new RuleSetId(evaluateRuleSetRequestDto.RuleSetId),
            Payload = evaluateRuleSetRequestDto.Payload,
            ExternalReference = evaluateRuleSetRequestDto.TransactionReference != null ? new ExternalReference(evaluateRuleSetRequestDto.TransactionReference) : null,
            CreateCase = new CreateCase(evaluateRuleSetRequestDto.CreateCase)
        };

        var response = await ruleSetService.EvaluateAsync(evaluateRuleSetRequest, HttpContext.RequestAborted);

        var resource = await evaluateResponseFactory.CreateAsync(response, context, HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.RuleSetsRead)]
    [RequirePermission(Permission.AuditLogsRead)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [HttpGet("{RuleSetId:guid}/history")]
    [ProducesResponseType<PagedResponseDto<AuditLogResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRuleSetHistory(Guid ruleSetId, [FromQuery] ListAuditLogRequestDto listAuditLogRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listAuditLogRequestDto.OrganizationId = authenticatedUser.OrganizationId;

        var auditLogs = await auditService.ListAsync(
            listAuditLogRequestDto.ToModel(AuditableEntityType.RuleSet, ruleSetId), HttpContext.RequestAborted);

        var resources = await auditLogResponseFactory.CreateAsync(
            auditLogs.Data, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(auditLogs.ToDto(resources));
    }
}