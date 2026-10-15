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
using Presentation.Mappers.Common;
using Presentation.Mappers.RuleEvaluations;
using Presentation.Models.Common;
using Presentation.Models.RuleEvaluations;

namespace Presentation.Controllers;

[RequirePermission(Permission.RuleEvaluationsRead)]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[ValidateAuthenticatedUser]
[Route("rule-evaluations")]
public class RuleEvaluationController(
    IApplicationUserService applicationUserService,
    IRuleEvaluationService ruleEvaluationService,
    RuleEvaluationResponseFactory ruleEvaluationResponseFactory,
    IValidator<RuleEvaluationRequestDto> ruleEvaluationRequestValidator,
    IValidator<ListRuleEvaluationRequestDto> listRuleEvaluationRequestValidator) : Controller
{
    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [HttpGet]
    [ProducesResponseType<PagedResponseDto<RuleEvaluationResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListRuleEvaluations([FromQuery] ListRuleEvaluationRequestDto listRuleEvaluationRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        listRuleEvaluationRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await listRuleEvaluationRequestValidator.ValidateAndThrowAsync(listRuleEvaluationRequestDto, HttpContext.RequestAborted);

        var ruleEvaluations = await ruleEvaluationService.ListAsync(listRuleEvaluationRequestDto.ToModel(), HttpContext.RequestAborted);

        var resources = await ruleEvaluationResponseFactory.CreateAsync(
            ruleEvaluations.Data, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(ruleEvaluations.ToDto(resources));
    }

    [Authorize(Policy = AuthConstants.DefaultPolicy)]
    [HttpGet("{RuleEvaluationId:guid}")]
    [ProducesResponseType<ResponseDataDto<RuleEvaluationResourceDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRuleEvaluation(RuleEvaluationRequestDto ruleEvaluationRequestDto)
    {
        var authenticatedUser = await applicationUserService.GetAsync(User, HttpContext.RequestAborted);

        ruleEvaluationRequestDto.OrganizationId = authenticatedUser.OrganizationId;
        await ruleEvaluationRequestValidator.ValidateAndThrowAsync(ruleEvaluationRequestDto, HttpContext.RequestAborted);

        var ruleEvaluation = await ruleEvaluationService.GetAsync(new RuleEvaluationId(ruleEvaluationRequestDto.RuleEvaluationId), HttpContext.RequestAborted);

        var resource = await ruleEvaluationResponseFactory.CreateAsync(
            ruleEvaluation, authenticatedUser.ToEnrichmentContext(User), HttpContext.RequestAborted);

        return Ok(resource.ToResponseData());
    }

}
