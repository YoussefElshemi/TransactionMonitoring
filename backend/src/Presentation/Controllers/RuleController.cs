using Core.Enums;
using Core.Interfaces.Services;
using Core.Mappers;
using Core.Models.Rules;
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
using Presentation.Mappers.Rules;
using Presentation.Models.AuditLogs;
using Presentation.Models.Common;
using Presentation.Models.Rules;

namespace Presentation.Controllers;

[ApiController]
[ValidateAuthenticatedUser]
[Route("rules")]
public class RuleController(
    IApplicationUserService applicationUserService,
    IRuleService ruleService,
    IRuleVersionService ruleVersionService,
    IRuleDeploymentService ruleDeploymentService,
    IAuditService auditService,
    RuleResponseFactory ruleResponseFactory,
    RuleDeploymentResponseFactory ruleDeploymentResponseFactory,
    AuditLogResponseFactory auditLogResponseFactory,
    EvaluateResponseFactory evaluateResponseFactory,
    IValidator<RuleBodyDto> ruleBodyValidator,
    IValidator<TestRuleRequestDto> testRuleRequestValidator,
    IValidator<RuleRequestDto> ruleRequestValidator,
    IValidator<RecoverRuleRequestDto> recoverRuleRequestValidator,
    IValidator<UpdateRuleRequestDto> updateRuleRequestValidator,
    IValidator<CreateRuleVersionRequestDto> createRuleVersionRequestValidator,
    IValidator<DeployRuleRequestDto> deployRuleRequestValidator,
    IValidator<CancelDeploymentRequestDto> cancelDeploymentRequestValidator,
    IValidator<EvaluateRuleRequestDto> evaluateRuleRequestValidator,
    IValidator<TransferRuleRequestDto> transferRuleRequestValidator,
    ITransactionSchemaService transactionSchemaService,
    IPartyMappingService partyMappingService,
    IVelocityProfileService velocityProfileService) : Controller
{
    [RequirePermission(Permission.RulesTest)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpPost("validate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidateRule([FromBody] RuleBodyDto createRuleRequestDto)
    {
        await ruleBodyValidator.ValidateAndThrowAsync(createRuleRequestDto, HttpContext.RequestAborted);

        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        await transactionSchemaService.EnforceRuleBodyAsync(
            authenticatedUser.OrganizationId, createRuleRequestDto.ToModel().ToModel(), HttpContext.RequestAborted);

        await partyMappingService.EnforceRuleBodyAsync(
            authenticatedUser.OrganizationId, createRuleRequestDto.ToModel().ToModel(), HttpContext.RequestAborted);

        await velocityProfileService.EnforceRuleBodyAsync(
            authenticatedUser.OrganizationId, createRuleRequestDto.ToModel().ToModel(), HttpContext.RequestAborted);

        return Ok();
    }

    [RequirePermission(Permission.RulesTest)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [SandboxMeterExempt]
    [HttpPost("test")]
    [ProducesResponseType<ResponseDataDto<TestRuleResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> TestRule(TestRuleRequestDto testRuleRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        await testRuleRequestValidator.ValidateAndThrowAsync(testRuleRequestDto, HttpContext.RequestAborted);

        var testRequest = new TestRuleRequest
        {
            RuleBody = testRuleRequestDto.RuleBody.ToModel(),
            Payload = testRuleRequestDto.Payload,
            MatchPolarity = Enum.Parse<RuleMatchPolarity>(testRuleRequestDto.MatchPolarity, true),
            Action = Enum.Parse<RuleAction>(testRuleRequestDto.Action, true),
            Severity = Enum.Parse<RuleSeverity>(testRuleRequestDto.Severity, true),
            RiskWeight = new RiskWeight(testRuleRequestDto.RiskWeight),
            OrganizationId = authenticatedUser.OrganizationId
        };

        var testRuleResponse = await ruleService.TestAsync(testRequest, HttpContext.RequestAborted);

        return Ok(new TestRuleResponseDto
        {
            Matched = testRuleResponse.Matched,
            Policy = testRuleResponse.Policy.ToDto(),
            Outcome = testRuleResponse.Outcome.ToDto()
        }.ToResponseData());
    }

    [RequirePermission(Permission.RulesRead)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [HttpGet("{RuleId:guid}")]
    [ProducesResponseType<ResponseDataDto<RuleResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRule(RuleRequestDto ruleRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        ruleRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await ruleRequestValidator.ValidateAndThrowAsync(ruleRequestDto, HttpContext.RequestAborted);

        var rule = await ruleService.GetAsync(new RuleId(ruleRequestDto.RuleId), HttpContext.RequestAborted);

        var resource = await ruleResponseFactory.CreateAsync(
            rule, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.RulesWrite)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpPatch("{RuleId:guid}")]
    [ProducesResponseType<ResponseDataDto<RuleResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateRule(UpdateRuleRequestDto updateRuleRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        updateRuleRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await updateRuleRequestValidator.ValidateAndThrowAsync(updateRuleRequestDto, HttpContext.RequestAborted);

        var rule = await ruleService.UpdateAsync(updateRuleRequestDto.ToModel(), HttpContext.RequestAborted);

        var resource = await ruleResponseFactory.CreateAsync(
            rule, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.RulesWrite)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpPost("{RuleId:guid}/versions")]
    [ProducesResponseType<ResponseDataDto<RuleVersionResponseDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateRuleVersion(CreateRuleVersionRequestDto createRuleVersionRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        createRuleVersionRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await createRuleVersionRequestValidator.ValidateAndThrowAsync(createRuleVersionRequestDto, HttpContext.RequestAborted);

        var ruleVersion = await ruleVersionService.CreateVersionAsync(createRuleVersionRequestDto.ToModel(authenticatedUser.UserId), HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, ruleVersion.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.RulesRead)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [HttpGet("{ruleId:guid}/versions")]
    [ProducesResponseType<PagedResponseDto<RuleVersionResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListRuleVersions(Guid ruleId, [FromQuery] ListRuleVersionRequestDto listRuleVersionRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listRuleVersionRequestDto.OrganizationId = authenticatedUser.OrganizationId;

        var ruleVersions = await ruleVersionService.ListAsync(listRuleVersionRequestDto.ToModel(new RuleId(ruleId)), HttpContext.RequestAborted);

        return Ok(ruleVersions.ToDto(RuleVersionDtoMapper.ToDto));
    }

    [RequirePermission(Permission.RulesRead)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [HttpGet("{RuleId:guid}/versions/{VersionNumber:int}")]
    [ProducesResponseType<ResponseDataDto<RuleVersionResponseDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRuleVersion(RuleVersionRequestDto ruleVersionRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        ruleVersionRequestDto.OrganizationId = authenticatedUser.OrganizationId;

        var ruleVersion = await ruleVersionService.GetVersionAsync(
            new RuleId(ruleVersionRequestDto.RuleId), new VersionNumber(ruleVersionRequestDto.VersionNumber), HttpContext.RequestAborted);

        return Ok(ruleVersion.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.RulesDeploy)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpPost("{RuleId:guid}/deployments")]
    [ProducesResponseType<ResponseDataDto<RuleDeploymentResourceDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> DeployRule(DeployRuleRequestDto deployRuleRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        deployRuleRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await deployRuleRequestValidator.ValidateAndThrowAsync(deployRuleRequestDto, HttpContext.RequestAborted);

        var deployment = await ruleDeploymentService.DeployAsync(deployRuleRequestDto.ToModel(authenticatedUser.UserId), HttpContext.RequestAborted);

        var resource = await ruleDeploymentResponseFactory.CreateAsync(
            deployment, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, resource.ToResponseData());
    }

    [RequirePermission(Permission.RulesRead)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [HttpGet("{ruleId:guid}/deployments")]
    [ProducesResponseType<PagedResponseDto<RuleDeploymentResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListRuleDeployments(Guid ruleId, [FromQuery] ListRuleDeploymentRequestDto listRuleDeploymentRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listRuleDeploymentRequestDto.OrganizationId = authenticatedUser.OrganizationId;

        var deployments = await ruleDeploymentService.ListAsync(listRuleDeploymentRequestDto.ToModel(new RuleId(ruleId)), HttpContext.RequestAborted);

        var resources = await ruleDeploymentResponseFactory.CreateAsync(
            deployments.Data, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(deployments.ToDto(resources));
    }

    [RequirePermission(Permission.RulesDeploy)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpPost("{RuleId:guid}/deployments/{DeploymentId:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> CancelRuleDeployment(CancelDeploymentRequestDto cancelDeploymentRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        cancelDeploymentRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await cancelDeploymentRequestValidator.ValidateAndThrowAsync(cancelDeploymentRequestDto, HttpContext.RequestAborted);

        await ruleDeploymentService.CancelDeploymentAsync(new RuleDeploymentId(cancelDeploymentRequestDto.DeploymentId), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.RulesWrite)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpDelete("{RuleId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteRule(RuleRequestDto ruleRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        ruleRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await ruleRequestValidator.ValidateAndThrowAsync(ruleRequestDto, HttpContext.RequestAborted);

        await ruleService.DeleteAsync(new RuleId(ruleRequestDto.RuleId), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.RulesWrite)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpPost("{RuleId:guid}/recover")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RecoverRule(RecoverRuleRequestDto recoverRuleRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        recoverRuleRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await recoverRuleRequestValidator.ValidateAndThrowAsync(recoverRuleRequestDto, HttpContext.RequestAborted);

        await ruleService.RecoverAsync(new RuleId(recoverRuleRequestDto.RuleId), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.RulesEvaluate)]
    [Authorize(Policy = AuthConstants.ApiScopePolicies.RulesEvaluate)]
    [RateLimit(RateLimitPool.Evaluation)]
    [Idempotent]
    [SandboxMeterExempt]
    [HttpPost("{RuleId:guid}/evaluate")]
    [ProducesResponseType<ResponseDataDto<EvaluateRuleResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> EvaluateRule(EvaluateRuleRequestDto evaluateRuleRequestDto)
    {
        var context = await applicationUserService.ResolveEnrichmentContextAsync(User, HttpContext.RequestAborted);

        evaluateRuleRequestDto.OrganizationId = context.OrganizationId;
        await evaluateRuleRequestValidator.ValidateAndThrowAsync(evaluateRuleRequestDto, HttpContext.RequestAborted);

        var evaluateRuleRequest = new EvaluateRuleRequest
        {
            RuleId = new RuleId(evaluateRuleRequestDto.RuleId),
            Payload = evaluateRuleRequestDto.Payload,
            ExternalReference = evaluateRuleRequestDto.TransactionReference != null ? new ExternalReference(evaluateRuleRequestDto.TransactionReference) : null,
            CreateCase = new CreateCase(evaluateRuleRequestDto.CreateCase)
        };

        var response = await ruleService.EvaluateAsync(evaluateRuleRequest, HttpContext.RequestAborted);

        var resource = await evaluateResponseFactory.CreateAsync(response, context, HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

    [RequirePermission(Permission.RulesWrite)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpPost("{RuleId:guid}/transfer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> TransferRule(TransferRuleRequestDto transferRuleRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        transferRuleRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await transferRuleRequestValidator.ValidateAndThrowAsync(transferRuleRequestDto, HttpContext.RequestAborted);

        await ruleService.MoveAsync(new RuleId(transferRuleRequestDto.RuleId), new RuleSetId(transferRuleRequestDto.Body.TargetRuleSetId), HttpContext.RequestAborted);

        return NoContent();
    }

    [RequirePermission(Permission.RulesWrite)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpPost("{RuleId:guid}/copy")]
    [ProducesResponseType<ResponseDataDto<RuleResourceDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CopyRule(TransferRuleRequestDto transferRuleRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        transferRuleRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await transferRuleRequestValidator.ValidateAndThrowAsync(transferRuleRequestDto, HttpContext.RequestAborted);

        var copyRuleRequest = new CopyRuleRequest
        {
            RuleId = new RuleId(transferRuleRequestDto.RuleId),
            TargetRuleSetId = new RuleSetId(transferRuleRequestDto.Body.TargetRuleSetId),
            CreatedByUserId = authenticatedUser.UserId
        };

        var rule = await ruleService.CopyRuleAsync(copyRuleRequest, HttpContext.RequestAborted);

        var resource = await ruleResponseFactory.CreateAsync(
            rule, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, resource.ToResponseData());
    }

    [RequirePermission(Permission.RulesRead)]
    [RequirePermission(Permission.AuditLogsRead)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [HttpGet("{RuleId:guid}/history")]
    [ProducesResponseType<PagedResponseDto<AuditLogResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRuleHistory(Guid ruleId, [FromQuery] ListAuditLogRequestDto listAuditLogRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listAuditLogRequestDto.OrganizationId = authenticatedUser.OrganizationId;

        var auditLogs = await auditService.ListAsync(
            listAuditLogRequestDto.ToModel(AuditableEntityType.Rule, ruleId), HttpContext.RequestAborted);

        var resources = await auditLogResponseFactory.CreateAsync(
            auditLogs.Data, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(auditLogs.ToDto(resources));
    }
}