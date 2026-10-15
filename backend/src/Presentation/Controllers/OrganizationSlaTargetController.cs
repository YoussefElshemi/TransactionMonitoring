using Core.Enums;
using Core.Interfaces.Services;
using Core.ValueObjects;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Attributes;
using Presentation.Constants;
using Presentation.Mappers.CaseSlaTargets;
using Presentation.Mappers.Common;
using Presentation.Models.CaseSlaTargets;
using Presentation.Models.Common;

namespace Presentation.Controllers;

[RequirePermission(Permission.SlaTargetsRead)]
[ApiController]
[Authorize(Policy = AuthConstants.DefaultPolicy)]
[Route("organizations/{OrganizationId:guid}/sla-targets")]
public class OrganizationSlaTargetController(
    ICaseService caseService,
    IValidator<OrganizationSlaTargetRequestDto> organizationSlaTargetRequestValidator,
    IValidator<SetOrganizationSlaTargetsRequestDto> setOrganizationSlaTargetsRequestValidator) : Controller
{
    [HttpGet]
    [ProducesResponseType<ResponseDataDto<SlaTargetResponseDto[]>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSlaTargets(OrganizationSlaTargetRequestDto organizationSlaTargetRequestDto)
    {
        await organizationSlaTargetRequestValidator.ValidateAndThrowAsync(organizationSlaTargetRequestDto, HttpContext.RequestAborted);

        var slaTargets = await caseService.GetSlaTargetsAsync(
            new OrganizationId(organizationSlaTargetRequestDto.OrganizationId), HttpContext.RequestAborted);

        return Ok(slaTargets.ToDto().ToResponseData());
    }

    [RequirePermission(Permission.SlaTargetsWrite)]
    [Idempotent]
    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetSlaTargets(SetOrganizationSlaTargetsRequestDto setOrganizationSlaTargetsRequestDto)
    {
        await setOrganizationSlaTargetsRequestValidator.ValidateAndThrowAsync(setOrganizationSlaTargetsRequestDto, HttpContext.RequestAborted);

        await caseService.SetSlaTargetsAsync(setOrganizationSlaTargetsRequestDto.ToModel(), HttpContext.RequestAborted);

        return NoContent();
    }
}
