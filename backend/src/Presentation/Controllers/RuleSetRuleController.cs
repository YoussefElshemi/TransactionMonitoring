using Core.Enums;
using Core.Interfaces.Services;
using Core.Models.Rules;
using Core.ValueObjects;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Extensions;
using Presentation.Factories;
using Presentation.Mappers.Common;
using Presentation.Mappers.Rules;
using Presentation.Models.Common;
using Presentation.Models.Rules;

namespace Presentation.Controllers;

[ApiController]
[Authorize]
[ValidateAuthenticatedUser]
[Route("rule-sets/{RuleSetId:guid}/rules")]
public class RuleSetRuleController(
    IApplicationUserService applicationUserService,
    IRuleService ruleService,
    RuleResponseFactory ruleResponseFactory,
    IValidator<CreateRuleRequestDto> createRuleRequestValidator,
    IValidator<ListRuleRequestDto> listRulesRequestValidator) : Controller
{
    [RequirePermission(Permission.RulesWrite)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [Idempotent]
    [HttpPost]
    [ProducesResponseType<ResponseDataDto<RuleResourceDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateRule(CreateRuleRequestDto createRuleRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        createRuleRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await createRuleRequestValidator.ValidateAndThrowAsync(createRuleRequestDto, HttpContext.RequestAborted);

        var createRuleRequest = new CreateRuleRequest
        {
            FriendlyName = new FriendlyName(createRuleRequestDto.Body.FriendlyName),
            RuleBody = createRuleRequestDto.Body.RuleBody.ToModel(),
            MatchPolarity = Enum.Parse<RuleMatchPolarity>(createRuleRequestDto.Body.MatchPolarity, true),
            Action = Enum.Parse<RuleAction>(createRuleRequestDto.Body.Action, true),
            Severity = Enum.Parse<RuleSeverity>(createRuleRequestDto.Body.Severity, true),
            RiskWeight = new RiskWeight(createRuleRequestDto.Body.RiskWeight),
            HoldWindow = createRuleRequestDto.Body.HoldWindow.HasValue ? new HoldWindow(createRuleRequestDto.Body.HoldWindow.Value) : null,
            HoldExpiryVerdict = createRuleRequestDto.Body.HoldExpiryVerdict != null ? Enum.Parse<HoldExpiryVerdict>(createRuleRequestDto.Body.HoldExpiryVerdict, true) : null,
            AlertSubjectRole = createRuleRequestDto.Body.AlertSubjectRole != null ? Enum.Parse<PartyRole>(createRuleRequestDto.Body.AlertSubjectRole, true) : null,
            DeduplicationWindow = createRuleRequestDto.Body.DeduplicationWindow.HasValue ? new DeduplicationWindow(createRuleRequestDto.Body.DeduplicationWindow.Value) : null,
            OrganizationId = authenticatedUser.OrganizationId,
            CreatedByUserId = authenticatedUser.UserId,
            IsEnabled = new IsEnabled(createRuleRequestDto.Body.IsEnabled),
            RuleSetId = new RuleSetId(createRuleRequestDto.RuleSetId)
        };

        var rule = await ruleService.CreateAsync(createRuleRequest, HttpContext.RequestAborted);

        var resource = await ruleResponseFactory.CreateAsync(
            rule, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return StatusCode(StatusCodes.Status201Created, resource.ToResponseData());
    }

    [RequirePermission(Permission.RulesRead)]
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [HttpGet]
    [ProducesResponseType<PagedResponseDto<RuleResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListRules([FromRoute] ListRuleRequestDto listRuleRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listRuleRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await listRulesRequestValidator.ValidateAndThrowAsync(listRuleRequestDto, HttpContext.RequestAborted);

        var rules = await ruleService.ListAsync(listRuleRequestDto.ToModel(), HttpContext.RequestAborted);

        var resources = await ruleResponseFactory.CreateAsync(
            rules.Data, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(rules.ToDto(resources));
    }
}